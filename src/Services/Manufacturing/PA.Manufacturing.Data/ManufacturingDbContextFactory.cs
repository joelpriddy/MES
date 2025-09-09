using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PA.Manufacturing.Data {
    public class ManufacturingDbContextFactory : IDesignTimeDbContextFactory<ManufacturingDbContext> {
        public ManufacturingDbContext CreateDbContext(string[] args) {
            var cs = Environment.GetEnvironmentVariable("PA_MANUFACTURING_CS")
                     ?? "server=localhost;port=3306;database=pa_mes_manufacturing;user=root;password=root";
            var opts = new DbContextOptionsBuilder<ManufacturingDbContext>()
                .UseMySql(cs, ServerVersion.AutoDetect(cs))
                .Options;
            return new ManufacturingDbContext(opts);
        }
    }
}
