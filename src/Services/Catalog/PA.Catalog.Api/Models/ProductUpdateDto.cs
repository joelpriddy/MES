namespace PA.Catalog.Api.Models
{
    public class ProductUpdateDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool? IsManufactured { get; set; }
        public string? UnitOfMeasure { get; set; }
        public string? Flavor { get; set; }
        public decimal? SizeLb { get; set; }
        public decimal? PriceRetail { get; set; }
        public decimal? PriceWholesale { get; set; }
        public bool? Active { get; set; }
    }
}
