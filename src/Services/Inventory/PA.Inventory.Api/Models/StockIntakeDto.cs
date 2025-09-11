namespace PA.Inventory.Api.Models
{
    public class StockIntakeDto
    {
        public long ProductId { get; set; }
        public long SiteId { get; set; }
        public string? LotNumber { get; set; }
        public DateTimeOffset? Expiration { get; set; }
        public decimal Quantity { get; set; }  // must be > 0
    }
}
