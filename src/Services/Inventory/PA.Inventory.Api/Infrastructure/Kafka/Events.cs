namespace PA.Inventory.Api.Infrastructure.Kafka
{
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
}
