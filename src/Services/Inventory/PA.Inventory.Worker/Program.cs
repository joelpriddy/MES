using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Confluent.Kafka;
using PA.Inventory.Data;
using PA.Inventory.Domain.Models;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;

namespace PA.Inventory.Worker
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((ctx, services) =>
                {
                    // EF
                    var cs = ctx.Configuration.GetConnectionString("Default")
                             ?? Environment.GetEnvironmentVariable("PA_INVENTORY_CS")
                             ?? "server=localhost;port=3306;database=pa_mes_inventory;user=root;password=root";

                    services.AddDbContext<InventoryDbContext>(opt =>
                        opt.UseMySql(cs, ServerVersion.AutoDetect(cs)));

                    // Options + hosted service
                    services.Configure<KafkaOptions>(ctx.Configuration.GetSection("Kafka"));
                    services.AddHostedService<InventoryConsumer>();
                })
                .Build();

            await host.RunAsync();
        }
    }

    public class KafkaOptions
    {
        public string BootstrapServers { get; set; } = "localhost:19092";
        public string GroupId { get; set; } = "inventory-worker";
        public string TopicOrderCreated { get; set; } = "orders.created";
        public string TopicOrderShipped { get; set; } = "orders.shipped";
    }

    public class OrderLinePayload
    {
        public long ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class OrderCreatedPayload
    {
        public long Id { get; set; }
        public long SiteId { get; set; }
        public List<OrderLinePayload> Lines { get; set; } = new();
    }

    public class OrderShippedPayload
    {
        public long Id { get; set; }
        public long SiteId { get; set; }
        public List<OrderLinePayload> Lines { get; set; } = new();
    }

    public class InventoryConsumer : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<InventoryConsumer> _logger;
        private readonly KafkaOptions _options;
        private IConsumer<string, string>? _consumer;
        private static readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

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
                    if (cr is null || string.IsNullOrWhiteSpace(cr.Message?.Value))
                    {
                        continue;
                    }

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

        private async Task HandleOrderCreatedAsync(OrderCreatedPayload evt, CancellationToken ct)
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

        private async Task HandleOrderShippedAsync(OrderShippedPayload evt, CancellationToken ct)
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
    }
}
