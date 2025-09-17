namespace PA.Platform.Messaging.Kafka
{
    public class KafkaSettings
    {
        public string BootstrapServers { get; set; } = "localhost:19092";
        public string GroupId { get; set; } = "default-group";
    }
}
