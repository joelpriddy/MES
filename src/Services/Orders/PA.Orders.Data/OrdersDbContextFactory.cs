using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PA.Orders.Data {
    public class OrdersDbContextFactory : IDesignTimeDbContextFactory<OrdersDbContext> {
        public OrdersDbContext CreateDbContext(string[] args) {
            var cs = Environment.GetEnvironmentVariable("PA_ORDERS_CS")
                     ?? "server=localhost;port=3306;database=pa_mes_orders;user=root;password=root";
            var opts = new DbContextOptionsBuilder<OrdersDbContext>()
                .UseMySql(cs, ServerVersion.AutoDetect(cs))
                .Options;
            return new OrdersDbContext(opts);
        }
    }
}
