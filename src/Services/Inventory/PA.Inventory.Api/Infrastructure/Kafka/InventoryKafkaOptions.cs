using PA.Platform.Messaging.Kafka;

namespace PA.Inventory.Api.Infrastructure.Kafka
{
    public class InventoryKafkaOptions : KafkaSettings
    {
        public string TopicOrderCreated { get; set; } = "orders.created";
        public string TopicOrderShipped { get; set; } = "orders.shipped";
        public string TopicOrderCanceled { get; set; } = "orders.canceled";
    }
}
