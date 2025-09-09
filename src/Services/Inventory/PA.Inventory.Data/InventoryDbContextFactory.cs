using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PA.Inventory.Data {
    public class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext> {
        public InventoryDbContext CreateDbContext(string[] args) {
            var cs = Environment.GetEnvironmentVariable("PA_INVENTORY_CS")
                     ?? "server=localhost;port=3306;database=pa_mes_inventory;user=root;password=root";
            var opts = new DbContextOptionsBuilder<InventoryDbContext>()
                .UseMySql(cs, ServerVersion.AutoDetect(cs))
                .Options;
            return new InventoryDbContext(opts);
        }
    }
}
