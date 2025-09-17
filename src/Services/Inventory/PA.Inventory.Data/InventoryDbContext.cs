using System;
using Microsoft.EntityFrameworkCore;
using PA.Inventory.Domain.Models;

namespace PA.Inventory.Data
{
    public class InventoryDbContext : DbContext
    {
        public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

        public DbSet<StockItem> StockItems => Set<StockItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<StockItem>(e =>
            {
                e.ToTable("stock_item");
                e.HasKey(x => x.Id);

                e.Property(x => x.LotNumber)
                    .HasMaxLength(64);

                e.Property(x => x.OnHand)
                    .HasPrecision(18, 3);

                e.Property(x => x.Reserved)
                    .HasPrecision(18, 3);

                e.HasIndex(x => new { x.ProductId, x.SiteId, x.LotNumber });

                e.Property(x => x.UpdatedOn)
                    .HasConversion(v => v, v => DateTime.SpecifyKind(v.DateTime, DateTimeKind.Utc));
            });
        }
    }
}
