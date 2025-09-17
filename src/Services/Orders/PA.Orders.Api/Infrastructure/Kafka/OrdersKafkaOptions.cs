using PA.Platform.Messaging.Kafka;

namespace PA.Orders.Api.Infrastructure.Kafka
{
    public class OrdersKafkaOptions : KafkaSettings
    {
        public string TopicOrderCreated { get; set; } = "orders.created"; 
        public string TopicOrderShipped { get; set; } = "orders.shipped";
        public string TopicOrderCanceled { get; set; } = "orders.canceled";
    }
}
