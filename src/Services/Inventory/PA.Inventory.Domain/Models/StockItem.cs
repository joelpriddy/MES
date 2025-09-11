namespace PA.Inventory.Domain.Models
{
    public class StockItem
    {
        public long Id { get; set; }

        // Links
        public long ProductId { get; set; }   // from Catalog
        public long SiteId { get; set; }      // multi-location support

        // Lot/expiry (for ingredients and resale goods)
        public string? LotNumber { get; set; }
        public DateTimeOffset? Expiration { get; set; }

        // Quantities
        public decimal OnHand { get; set; }    // total available at site/lot
        public decimal Reserved { get; set; }  // held for orders

        public DateTimeOffset UpdatedOn { get; set; } = DateTimeOffset.UtcNow;
    }
}
