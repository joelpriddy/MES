using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PA.Customers.Data {
    public class CustomersDbContextFactory : IDesignTimeDbContextFactory<CustomersDbContext> {
        public CustomersDbContext CreateDbContext(string[] args) {
            var cs = Environment.GetEnvironmentVariable("PA_CUSTOMERS_CS")
                     ?? "server=localhost;port=3306;database=pa_mes_customers;user=root;password=root";
            var opts = new DbContextOptionsBuilder<CustomersDbContext>()
                .UseMySql(cs, ServerVersion.AutoDetect(cs))
                .Options;
            return new CustomersDbContext(opts);
        }
    }
}
