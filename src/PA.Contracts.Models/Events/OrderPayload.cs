namespace PA.Contracts.Models.Events
{
    public class OrderPayload
    {
        // Helpful metadata
        public string EventType { get; set; } = "";// e.g., "orders.created", "orders.shipped", "orders.canceled"
        public string SchemaVersion { get; set; } = "1"; // simple versioning

        public long Id { get; set; }
        public long SiteId { get; set; }
        public long CustomerId { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = "";
        public DateTimeOffset? CreatedOn { get; set; }
        public DateTimeOffset? PaidOn { get; set; }
        public DateTimeOffset? ShippedOn { get; set; }
        public DateTimeOffset? CanceledOn { get; set; }

        public List<OrderLinePayload> Lines { get; set; } = new();
    }
}
