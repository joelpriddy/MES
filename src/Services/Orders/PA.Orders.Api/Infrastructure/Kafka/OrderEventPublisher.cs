using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using PA.Orders.Domain.Models;

namespace PA.Orders.Api.Infrastructure.Kafka
{
    public interface IOrderEventPublisher : IDisposable
    {
        Task PublishOrderCreatedAsync(Order order, CancellationToken ct = default);
        Task PublishOrderShippedAsync(Order order, CancellationToken ct = default);
    }

    public class OrderEventPublisher : IOrderEventPublisher
    {
        private readonly IProducer<string, string> _producer;
        private readonly KafkaOptions _options;
        private readonly JsonSerializerOptions _json = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public OrderEventPublisher(IOptions<KafkaOptions> options)
        {
            _options = options.Value;

            var config = new ProducerConfig
            {
                BootstrapServers = _options.BootstrapServers,
                Acks = Acks.All,
                EnableIdempotence = true
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
        }

        public async Task PublishOrderCreatedAsync(Order order, CancellationToken ct = default)
        {
            var payload = new
            {
                id = order.Id,
                customerId = order.CustomerId,
                siteId = order.SiteId,
                total = order.Total,
                status = order.Status,
                createdOn = order.CreatedOn,
                lines = order.Lines.Select(l => new
                {
                    productId = l.ProductId,
                    quantity = l.Quantity,
                    unitPrice = l.UnitPrice
                }).ToList()
            };
            var value = JsonSerializer.Serialize(payload, _json);
            var message = new Message<string, string> { Key = order.Id.ToString(), Value = value };
            var result = await _producer.ProduceAsync(_options.TopicOrderCreated, message, ct);
        }

        public async Task PublishOrderShippedAsync(Order order, CancellationToken ct = default)
        {
            var payload = new
            {
                id = order.Id,
                customerId = order.CustomerId,
                siteId = order.SiteId,
                total = order.Total,
                status = order.Status,
                shippedOn = order.ShippedOn
            };
            var value = JsonSerializer.Serialize(payload, _json);
            var message = new Message<string, string> { Key = order.Id.ToString(), Value = value };
            var result = await _producer.ProduceAsync(_options.TopicOrderShipped, message, ct);
        }

        public void Dispose()
        {
            _producer?.Flush(TimeSpan.FromSeconds(2));
            _producer?.Dispose();
        }
    }
}
