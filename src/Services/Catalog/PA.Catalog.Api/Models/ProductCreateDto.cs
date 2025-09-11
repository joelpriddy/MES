namespace PA.Catalog.Api.Models
{
    public class ProductCreateDto
    {
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public bool IsManufactured { get; set; }
        public string UnitOfMeasure { get; set; } = "lb";
        public string? Flavor { get; set; }
        public decimal? SizeLb { get; set; }

        public decimal PriceRetail { get; set; }
        public decimal? PriceWholesale { get; set; }
        public bool Active { get; set; } = true;
    }
}

