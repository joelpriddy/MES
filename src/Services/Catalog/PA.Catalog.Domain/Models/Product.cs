using System;

namespace PA.Catalog.Domain.Models
{
    public class Product
    {
        public long Id { get; set; }

        // Identification
        public string Sku { get; set; } = string.Empty;   // unique
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Classification / options
        public bool IsManufactured { get; set; }          // manufactured vs. resale
        public string UnitOfMeasure { get; set; } = "lb"; // default pounds
        public string? Subtype { get; set; }               // lemongrass, peppermint
        public decimal? SizeLb { get; set; }              // 5 or 10

        // Pricing (retail/wholesale tiers)
        public decimal PriceRetail { get; set; }
        public decimal? PriceWholesale { get; set; }

        // State
        public bool Active { get; set; } = true;
        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedOn { get; set; }
    }
}
