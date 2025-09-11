using Microsoft.EntityFrameworkCore;
using PA.Catalog.Domain.Models;

namespace PA.Catalog.Data
{
    public class CatalogDbContext : DbContext
    {
        public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

        public DbSet<Product> Products => Set<Product>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>(e =>
            {
                e.ToTable("product");

                e.HasKey(x => x.Id);

                e.Property(x => x.Sku)
                    .IsRequired()
                    .HasMaxLength(64);
                e.HasIndex(x => x.Sku).IsUnique();

                e.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(256);

                e.Property(x => x.Description)
                    .HasMaxLength(4000);

                e.Property(x => x.UnitOfMeasure)
                    .IsRequired()
                    .HasMaxLength(16);

                e.Property(x => x.Flavor)
                    .HasMaxLength(64);

                e.Property(x => x.SizeLb)
                    .HasPrecision(18, 3);

                e.Property(x => x.PriceRetail)
                    .HasPrecision(18, 2);

                e.Property(x => x.PriceWholesale)
                    .HasPrecision(18, 2);

                e.Property(x => x.Active)
                    .HasDefaultValue(true);

                e.Property(x => x.CreatedOn)
                    .HasConversion(v => v, v => DateTime.SpecifyKind(v.DateTime, DateTimeKind.Utc));

                e.Property(x => x.UpdatedOn)
                    .HasConversion(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value.DateTime, DateTimeKind.Utc) : (DateTime?)null);
            });
        }
    }
}
