using PA.Catalog.Domain.Models;

namespace PA.Catalog.Api.Models
{
    public class ProductDto
    {
        public long Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsManufactured { get; set; }
        public string UnitOfMeasure { get; set; } = "lb";
        public string? Flavor { get; set; }
        public decimal? SizeLb { get; set; }
        public decimal PriceRetail { get; set; }
        public decimal? PriceWholesale { get; set; }
        public bool Active { get; set; }

        public ProductDto(Product model)
        {
            Id = model.Id;
            Sku = model.Sku;
            Name = model.Name;
            Description = model.Description;
            IsManufactured = model.IsManufactured;
            UnitOfMeasure = model.UnitOfMeasure;
            Flavor = model.Flavor;
            SizeLb = model.SizeLb;
            PriceRetail = model.PriceRetail;
            PriceWholesale = model.PriceWholesale;
            Active = model.Active;
        }
    }
}
