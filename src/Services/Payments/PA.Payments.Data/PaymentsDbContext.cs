using Microsoft.EntityFrameworkCore;

namespace PA.Payments.Data {
    public class PaymentsDbContext : DbContext {
        public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            // TODO: configure entities (EF Core Fluent API)
        }
        // TODO: add DbSet<T> for your entities, e.g.:
        // public DbSet<Product> Products => Set<Product>();
    }
}
