using Microsoft.EntityFrameworkCore;
using PA.Inventory.Data;
using PA.Inventory.Worker.Kafka;

namespace PA.Inventory.Worker
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((ctx, services) =>
                {
                    // EF
                    var cs = ctx.Configuration.GetConnectionString("Default")
                             ?? Environment.GetEnvironmentVariable("PA_INVENTORY_CS")
                             ?? "server=localhost;port=3306;database=pa_mes_inventory;user=root;password=root";

                    services.AddDbContext<InventoryDbContext>(opt =>
                        opt.UseMySql(cs, ServerVersion.AutoDetect(cs)));

                    // Options + hosted service
                    services.Configure<InventoryKafkaOptions>(ctx.Configuration.GetSection("Kafka"));
                    services.AddHostedService<InventoryConsumer>();
                })
                .Build();

            await host.RunAsync();
        }
    }
}
