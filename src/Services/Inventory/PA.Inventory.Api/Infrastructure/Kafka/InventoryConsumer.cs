using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PA.Inventory.Api.Infrastructure.Kafka;
using PA.Inventory.Data;
using PA.Inventory.Domain.Models;

namespace PA.Inventory.Api.Infrastructure.Kafka
{
    /// <summary>
    /// Consumes orders.created and orders.shipped and updates StockItem
    /// - orders.created: increases Reserved (up to available OnHand) by product/site across lots (FEFO).
    /// - orders.shipped: decreases Reserved and OnHand by shipped quantity across lots (FEFO).
    /// </summary>
    public class InventoryConsumer : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<InventoryConsumer> _logger;
        private readonly KafkaOptions _options;
        private readonly IConsumer<string, string> _consumer;
        private static readonly JsonSerializerOptions _json = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public InventoryConsumer(IServiceProvider services, IOptions<KafkaOptions> options, ILogger<InventoryConsumer> logger)
        {
            _services = services;
            _logger = logger;
            _options = options.Value;

            var config = new ConsumerConfig
            {
                BootstrapServers = _options.BootstrapServers,
                GroupId = _options.GroupId,
                EnableAutoCommit = true,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            _consumer = new ConsumerBuilder<string, string>(config).Build();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _consumer.Subscribe(new[] { _options.TopicOrderCreated, _options.TopicOrderShipped });
            _logger.LogInformation("InventoryConsumer subscribed to topics: {Topics}", string.Join(", ", _options.TopicOrderCreated, _options.TopicOrderShipped));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var cr = _consumer.Consume(stoppingToken);

                    if (cr is null || string.IsNullOrWhiteSpace(cr.Message?.Value)) { continue; }

                    if (string.Equals(cr.Topic, _options.TopicOrderCreated, StringComparison.OrdinalIgnoreCase))
                    {
                        var payload = JsonSerializer.Deserialize<OrderCreatedPayload>(cr.Message.Value, _json);

                        if (payload is not null) { await HandleOrderCreatedAsync(payload, stoppingToken); }
                    }
                    else if (string.Equals(cr.Topic, _options.TopicOrderShipped, StringComparison.OrdinalIgnoreCase))
                    {
                        var payload = JsonSerializer.Deserialize<OrderShippedPayload>(cr.Message.Value, _json);

                        if (payload is not null) { await HandleOrderShippedAsync(payload, stoppingToken); }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ConsumeException ce)
                {
                    _logger.LogError(ce, "Kafka consume error: {Reason}", ce.Error.Reason);
                    await Task.Delay(500, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error in InventoryConsumer loop.");
                    await Task.Delay(500, stoppingToken);
                }
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

                    // FEFO: earliest expiration first (nulls last), then oldest updated.
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
                        _logger.LogWarning("Order {OrderId}: insufficient stock to reserve ProductId={ProductId} at SiteId={SiteId}. Unreserved={Remaining}",
                            evt.Id, line.ProductId, evt.SiteId, remaining);
                    }
                }

                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                _logger.LogError(ex, "Failed to apply reservations for OrderCreated {OrderId}", evt.Id);
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

                    // FEFO: earliest expiration first (nulls last), then oldest updated.
                    var lots = await db.StockItems
                        .Where(x => x.ProductId == line.ProductId && x.SiteId == evt.SiteId)
                        .OrderBy(x => x.Expiration.HasValue ? 0 : 1)
                        .ThenBy(x => x.Expiration)
                        .ThenBy(x => x.UpdatedOn)
                        .ToListAsync(ct);

                    foreach (var lot in lots)
                    {
                        if (toShip <= 0) { break; }

                        // First release reservations up to min(Reserved, toShip).
                        var release = Math.Min(lot.Reserved, toShip);

                        if (release > 0)
                        {
                            lot.Reserved -= release;
                            toShip -= release;
                        }

                        // Then reduce OnHand for any remaining toShip from this lot.
                        if (toShip > 0 && lot.OnHand > 0)
                        {
                            var decrement = Math.Min(lot.OnHand, toShip);
                            lot.OnHand -= decrement;
                            toShip -= decrement;
                        }

                        lot.UpdatedOn = DateTimeOffset.UtcNow;
                    }

                    if (toShip > 0)
                    {
                        _logger.LogWarning("Order {OrderId}: could not ship full qty for ProductId={ProductId} at SiteId={SiteId}. Shortfall={Shortfall}",
                            evt.Id, line.ProductId, evt.SiteId, toShip);
                    }
                }

                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                _logger.LogError(ex, "Failed to apply stock decrement for OrderShipped {OrderId}", evt.Id);
            }
        }

        public override void Dispose()
        {
            try
            {
                _consumer?.Close();
            }
            catch { }

            _consumer?.Dispose();
            base.Dispose();
        }
    }
}
