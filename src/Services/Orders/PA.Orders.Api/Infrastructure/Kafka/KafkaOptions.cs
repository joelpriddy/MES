namespace PA.Orders.Api.Infrastructure.Kafka
{
    public class KafkaOptions
    {
        public string BootstrapServers { get; set; } = "localhost:19092";
        public string TopicOrderCreated { get; set; } = "orders.created"; 
        public string TopicOrderShipped { get; set; } = "orders.shipped";
    }
}
