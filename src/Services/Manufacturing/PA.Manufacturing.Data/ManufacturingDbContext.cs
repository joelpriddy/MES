using Microsoft.EntityFrameworkCore;

namespace PA.Manufacturing.Data {
    public class ManufacturingDbContext : DbContext {
        public ManufacturingDbContext(DbContextOptions<ManufacturingDbContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            // TODO: configure entities (EF Core Fluent API)
        }
        // TODO: add DbSet<T> for your entities, e.g.:
        // public DbSet<Product> Products => Set<Product>();
    }
}
