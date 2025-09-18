using Microsoft.EntityFrameworkCore;
using PA.Catalog.Domain.Models;
using System.Threading.Tasks;

namespace PA.Catalog.Data.Seed
{
    public static class CatalogSeeder
    {
        public static async Task SeedAsync(CatalogDbContext db)
        {
            // Only seed when table is empty
            if (await db.Products.AnyAsync())
            {
                return;
            }

            var items = new List<Product>
            {
                new Product
                {
                    Sku = "FON-5-LEM",
                    Name = "Fondant 5lb - Lemongrass",
                    Description = "Small-batch honeybee fondant, lemongrass flavor",
                    IsManufactured = true,
                    UnitOfMeasure = "lb",
                    Subtype = "lemongrass",
                    SizeLb = 5m,
                    PriceRetail = 14.99m,
                    PriceWholesale = 12.50m,
                    Active = true
                },
                new Product
                {
                    Sku = "FON-10-LEM",
                    Name = "Fondant 10lb - Lemongrass",
                    Description = "Small-batch honeybee fondant, lemongrass flavor",
                    IsManufactured = true,
                    UnitOfMeasure = "lb",
                    Subtype = "lemongrass",
                    SizeLb = 10m,
                    PriceRetail = 27.99m,
                    PriceWholesale = 24.00m,
                    Active = true
                },
                new Product
                {
                    Sku = "FON-5-PEP",
                    Name = "Fondant 5lb - Peppermint",
                    Description = "Small-batch honeybee fondant, peppermint flavor",
                    IsManufactured = true,
                    UnitOfMeasure = "lb",
                    Subtype = "peppermint",
                    SizeLb = 5m,
                    PriceRetail = 14.99m,
                    PriceWholesale = 12.50m,
                    Active = true
                },
                new Product
                {
                    Sku = "FON-10-PEP",
                    Name = "Fondant 10lb - Peppermint",
                    Description = "Small-batch honeybee fondant, peppermint flavor",
                    IsManufactured = true,
                    UnitOfMeasure = "lb",
                    Subtype = "peppermint",
                    SizeLb = 10m,
                    PriceRetail = 27.99m,
                    PriceWholesale = 24.00m,
                    Active = true
                }
            };

            await db.Products.AddRangeAsync(items);
            await db.SaveChangesAsync();
        }
    }
}
