using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PA.Catalog.Data {
    public class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext> {
        public CatalogDbContext CreateDbContext(string[] args) {
            var cs = Environment.GetEnvironmentVariable("PA_CATALOG_CS")
                     ?? "server=localhost;port=3306;database=pa_mes_catalog;user=root;password=root";
            var opts = new DbContextOptionsBuilder<CatalogDbContext>()
                .UseMySql(cs, ServerVersion.AutoDetect(cs))
                .Options;
            return new CatalogDbContext(opts);
        }
    }
}
