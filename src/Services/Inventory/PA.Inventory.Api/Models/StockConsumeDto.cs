namespace PA.Inventory.Api.Models
{
    public class StockConsumeDto
    {
        public long ProductId { get; set; }
        public long SiteId { get; set; }
        public string? LotNumber { get; set; }   // if omitted, we’ll pick the earliest lot
        public decimal Quantity { get; set; }    // must be > 0
    }
}
