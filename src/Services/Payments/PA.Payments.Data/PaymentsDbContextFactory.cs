using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PA.Payments.Data {
    public class PaymentsDbContextFactory : IDesignTimeDbContextFactory<PaymentsDbContext> {
        public PaymentsDbContext CreateDbContext(string[] args) {
            var cs = Environment.GetEnvironmentVariable("PA_PAYMENTS_CS")
                     ?? "server=localhost;port=3306;database=pa_mes_payments;user=root;password=root";
            var opts = new DbContextOptionsBuilder<PaymentsDbContext>()
                .UseMySql(cs, ServerVersion.AutoDetect(cs))
                .Options;
            return new PaymentsDbContext(opts);
        }
    }
}
