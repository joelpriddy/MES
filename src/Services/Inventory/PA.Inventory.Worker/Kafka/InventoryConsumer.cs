using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PA.Contracts.Models.Events;
using PA.Inventory.Data;

namespace PA.Inventory.Worker.Kafka
{
    public class InventoryConsumer : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<InventoryConsumer> _logger;
        private readonly InventoryKafkaOptions _options;
        private IConsumer<string, string>? _consumer;
        private static readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public InventoryConsumer(IServiceProvider services, IOptions<InventoryKafkaOptions> options, ILogger<InventoryConsumer> logger)
        {
            _services = services;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var topics = new[] { _options.TopicOrderCreated, _options.TopicOrderShipped, _options.TopicOrderCanceled };

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_consumer is null)
                    {
                        try
                        {
                            var config = new ConsumerConfig
                            {
                                BootstrapServers = _options.BootstrapServers,
                                GroupId = _options.GroupId,
                                EnableAutoCommit = true,
                                AutoOffsetReset = AutoOffsetReset.Earliest,
                                BrokerAddressFamily = BrokerAddressFamily.V4,
                                SocketKeepaliveEnable = true
                            };
                            _consumer = new ConsumerBuilder<string, string>(config).Build();
                            _consumer.Subscribe(topics);
                            _logger.LogInformation("Worker subscribed to topics: {Topics}", string.Join(", ", topics));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Kafka unavailable at {Servers}; retrying soon.", _options.BootstrapServers);
                            _consumer = null;
                            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                            continue;
                        }
                    }

                    var cr = _consumer.Consume(TimeSpan.FromMilliseconds(250));

                    if (cr is null || string.IsNullOrWhiteSpace(cr.Message?.Value)) { continue; }

                    if (string.Equals(cr.Topic, _options.TopicOrderCreated, StringComparison.OrdinalIgnoreCase))
                    {
                        var payload = JsonSerializer.Deserialize<OrderPayload>(cr.Message.Value, _json);
                        if (payload is not null) { await HandleOrderCreatedAsync(payload, stoppingToken); }
                    }
                    else if (string.Equals(cr.Topic, _options.TopicOrderShipped, StringComparison.OrdinalIgnoreCase))
                    {
                        var payload = JsonSerializer.Deserialize<OrderPayload>(cr.Message.Value, _json);
                        if (payload is not null) { await HandleOrderShippedAsync(payload, stoppingToken); }
                    }
                    else if (string.Equals(cr.Topic, _options.TopicOrderCanceled, StringComparison.OrdinalIgnoreCase))
                    {
                        var payload = JsonSerializer.Deserialize<OrderPayload>(cr.Message.Value, _json);
                        if (payload is not null) { await HandleOrderCanceledAsync(payload, stoppingToken); }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ConsumeException ce)
                {
                    _logger.LogWarning(ce, "Kafka consume warning: {Reason}", ce.Error.Reason);
                    if (ce.Error.IsFatal)
                    {
                        try { _consumer?.Close(); } catch { }
                        try { _consumer?.Dispose(); } catch { }
                        _consumer = null;
                        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error in worker; will retry.");
                    try { _consumer?.Close(); } catch { }
                    try { _consumer?.Dispose(); } catch { }
                    _consumer = null;
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }

            try { _consumer?.Close(); } catch { }
            try { _consumer?.Dispose(); } catch { }
        }

        private async Task HandleOrderCreatedAsync(OrderPayload evt, CancellationToken ct)
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

            await using var tx = await db.Database.BeginTransactionAsync(ct);
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
            }
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }

        private async Task HandleOrderShippedAsync(OrderPayload evt, CancellationToken ct)
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
                        if (remaining <= 0) { break; }

                        // Ship from RESERVED first: reduce BOTH Reserved and OnHand.
                        var fromReserved = Math.Min(lot.Reserved, remaining);
                        if (fromReserved > 0)
                        {
                            lot.Reserved -= fromReserved;
                            lot.OnHand -= fromReserved;
                            remaining -= fromReserved;
                        }

                        // Then (if needed) ship from free OnHand.
                        if (remaining > 0 && lot.OnHand > 0)
                        {
                            var fromOnHand = Math.Min(lot.OnHand, remaining);
                            lot.OnHand -= fromOnHand;
                            remaining -= fromOnHand;
                        }

                        lot.UpdatedOn = DateTimeOffset.UtcNow;
                    }

                    if (remaining > 0)
                    {
                        _logger.LogWarning(
                            "Order {OrderId}: shortfall shipping ProductId={ProductId} at SiteId={SiteId}. Remaining={Remaining}",
                            evt.Id, line.ProductId, evt.SiteId, remaining);
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

        private async Task HandleOrderCanceledAsync(OrderPayload evt, CancellationToken ct)
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
                        if (remaining <= 0) { break; }

                        var release = Math.Min(lot.Reserved, remaining);
                        if (release > 0)
                        {
                            lot.Reserved -= release;
                            remaining -= release;
                            lot.UpdatedOn = DateTimeOffset.UtcNow;
                        }
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
    }
}
