namespace PA.Inventory.Api.Infrastructure.Kafka
{
    public class KafkaOptions
    {
        public string BootstrapServers { get; set; } = "localhost:19092";
        public string GroupId { get; set; } = "inventory-api";
        public string TopicOrderCreated { get; set; } = "orders.created";
        public string TopicOrderShipped { get; set; } = "orders.shipped";
    }
}
