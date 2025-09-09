using Microsoft.EntityFrameworkCore;

namespace PA.Orders.Data {
    public class OrdersDbContext : DbContext {
        public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            // TODO: configure entities (EF Core Fluent API)
        }
        // TODO: add DbSet<T> for your entities, e.g.:
        // public DbSet<Product> Products => Set<Product>();
    }
}
