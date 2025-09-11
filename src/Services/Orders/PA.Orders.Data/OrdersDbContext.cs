using Microsoft.EntityFrameworkCore;
using PA.Orders.Domain.Models;

namespace PA.Orders.Data
{
    public class OrdersDbContext : DbContext
    {
        public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }

        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderLine> OrderLines => Set<OrderLine>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Order>(e =>
            {
                e.ToTable("order_hdr");
                e.HasKey(x => x.Id);

                e.Property(x => x.Status).HasMaxLength(32);
                e.Property(x => x.Total).HasPrecision(18, 2);

                e.Property(x => x.CreatedOn)
                    .HasConversion(v => v, v => DateTime.SpecifyKind(v.DateTime, DateTimeKind.Utc));

                e.Property(x => x.PaidOn)
                    .HasConversion(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value.DateTime, DateTimeKind.Utc) : (DateTime?)null);

                e.Property(x => x.ShippedOn)
                    .HasConversion(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value.DateTime, DateTimeKind.Utc) : (DateTime?)null);

                e.HasMany(x => x.Lines)
                    .WithOne()
                    .HasForeignKey(l => l.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OrderLine>(e =>
            {
                e.ToTable("order_line");
                e.HasKey(x => x.Id);

                e.Property(x => x.Quantity).HasPrecision(18, 3);
                e.Property(x => x.UnitPrice).HasPrecision(18, 2);

                e.HasIndex(x => new { x.OrderId, x.ProductId });
            });
        }
    }
}
