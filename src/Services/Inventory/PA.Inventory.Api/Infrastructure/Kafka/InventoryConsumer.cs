using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PA.Inventory.Data;

namespace PA.Inventory.Api.Infrastructure.Kafka
{
    /// <summary>
    /// Consumes orders.created and orders.shipped and updates StockItem.
    /// Resilient: if Kafka is down, it logs and retries; it never prevents the web app from starting.
    /// </summary>
    public class InventoryConsumer : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<InventoryConsumer> _logger;
        private readonly KafkaOptions _options;
        private IConsumer<string, string>? _consumer;

        private static readonly JsonSerializerOptions _json = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public InventoryConsumer(IServiceProvider services, IOptions<KafkaOptions> options, ILogger<InventoryConsumer> logger)
        {
            _services = services;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var topics = new[] { _options.TopicOrderCreated, _options.TopicOrderShipped };

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_consumer is null)
                    {
                        TryCreateConsumer(topics);
                    }

                    if (_consumer is null)
                    {
                        // Could not create yet — wait a bit and retry
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                        continue;
                    }

                    // Non-blocking poll (short timeout) so we can shutdown fast
                    var cr = _consumer.Consume(TimeSpan.FromMilliseconds(250));

                    if (cr is null || string.IsNullOrWhiteSpace(cr.Message?.Value))
                    {
                        continue;
                    }

                    if (string.Equals(cr.Topic, _options.TopicOrderCreated, StringComparison.OrdinalIgnoreCase))
                    {
                        var payload = JsonSerializer.Deserialize<OrderCreatedPayload>(cr.Message.Value, _json);

                        if (payload is not null)
                        {
                            await HandleOrderCreatedAsync(payload, stoppingToken);
                        }
                    }
                    else if (string.Equals(cr.Topic, _options.TopicOrderShipped, StringComparison.OrdinalIgnoreCase))
                    {
                        var payload = JsonSerializer.Deserialize<OrderShippedPayload>(cr.Message.Value, _json);

                        if (payload is not null)
                        {
                            await HandleOrderShippedAsync(payload, stoppingToken);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // shutting down
                    break;
                }
                catch (ConsumeException ce)
                {
                    _logger.LogWarning(ce, "Kafka consume warning: {Reason}", ce.Error.Reason);
                    // If fatal, drop consumer and recreate
                    if (ce.Error.IsFatal)
                    {
                        CloseConsumer();
                        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error in InventoryConsumer; will retry.");
                    CloseConsumer();
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }

            CloseConsumer();
        }

        private void TryCreateConsumer(string[] topics)
        {
            try
            {
                var config = new ConsumerConfig
                {
                    BootstrapServers = _options.BootstrapServers,
                    GroupId = _options.GroupId,
                    EnableAutoCommit = true,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    // These help on Windows localhost setups:
                    BrokerAddressFamily = BrokerAddressFamily.V4,
                    SocketKeepaliveEnable = true
                };

                _consumer = new ConsumerBuilder<string, string>(config).Build();
                _consumer.Subscribe(topics);
                _logger.LogInformation("InventoryConsumer subscribed to topics: {Topics}", string.Join(", ", topics));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kafka unavailable at {Servers}; will retry shortly.", _options.BootstrapServers);
                CloseConsumer();
            }
        }

        private async Task HandleOrderCreatedAsync(OrderCreatedPayload evt, CancellationToken ct)
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

            await using var tx = await db.Database.BeginTransactionAsync(ct);

            try
            {
                foreach (var line in evt.Lines)
                {
                    var remaining = line.Quantity;

                    if (remaining <= 0) { continue; }

                    var lots = await db.StockItems
                        .Where(x => x.ProductId == line.ProductId && x.SiteId == evt.SiteId)
                        .OrderBy(x => x.Expiration.HasValue ? 0 : 1)
                        .ThenBy(x => x.Expiration)
                        .ThenBy(x => x.UpdatedOn)
                        .ToListAsync(ct);

                    foreach (var lot in lots)
                    {
                        var available = lot.OnHand - lot.Reserved;

                        if (available <= 0) { continue; }

                        var reserve = Math.Min(available, remaining);

                        lot.Reserved += reserve;
                        lot.UpdatedOn = DateTimeOffset.UtcNow;

                        remaining -= reserve;

                        if (remaining <= 0) { break; }
                    }

                    if (remaining > 0)
                    {
                        // Not enough stock to reserve all; log and continue
                        // (Business rule: we *do not* block the order here)
                    }
                }

                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        private async Task HandleOrderShippedAsync(OrderShippedPayload evt, CancellationToken ct)
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

            await using var tx = await db.Database.BeginTransactionAsync(ct);

            try
            {
                foreach (var line in evt.Lines)
                {
                    var toShip = line.Quantity;
                    if (toShip <= 0) { continue; }

                    var lots = await db.StockItems
                        .Where(x => x.ProductId == line.ProductId && x.SiteId == evt.SiteId)
                        .OrderBy(x => x.Expiration.HasValue ? 0 : 1)
                        .ThenBy(x => x.Expiration)
                        .ThenBy(x => x.UpdatedOn)
                        .ToListAsync(ct);

                    foreach (var lot in lots)
                    {
                        if (toShip <= 0) { break; }

                        var release = Math.Min(lot.Reserved, toShip);
                        if (release > 0)
                        {
                            lot.Reserved -= release;
                            toShip -= release;
                        }

                        if (toShip > 0 && lot.OnHand > 0)
                        {
                            var decrement = Math.Min(lot.OnHand, toShip);
                            lot.OnHand -= decrement;
                            toShip -= decrement;
                        }

                        lot.UpdatedOn = DateTimeOffset.UtcNow;
                    }
                }

                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        private void CloseConsumer()
        {
            try { _consumer?.Close(); } catch { /* ignore */ }
            try { _consumer?.Dispose(); } catch { /* ignore */ }

            _consumer = null;
        }
    }
}
