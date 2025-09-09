using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PA.Shipping.Data {
    public class ShippingDbContextFactory : IDesignTimeDbContextFactory<ShippingDbContext> {
        public ShippingDbContext CreateDbContext(string[] args) {
            var cs = Environment.GetEnvironmentVariable("PA_SHIPPING_CS")
                     ?? "server=localhost;port=3306;database=pa_mes_shipping;user=root;password=root";
            var opts = new DbContextOptionsBuilder<ShippingDbContext>()
                .UseMySql(cs, ServerVersion.AutoDetect(cs))
                .Options;
            return new ShippingDbContext(opts);
        }
    }
}
