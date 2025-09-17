using Microsoft.EntityFrameworkCore;
using PA.Inventory.Data;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.OpenApi.Models;
using PA.Inventory.Api.Endpoints;
using PA.Inventory.Api.Infrastructure.Kafka;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(o => { o.ListenAnyIP(5081, lo => lo.Protocols = HttpProtocols.Http1AndHttp2); });
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
builder.Services.Configure<InventoryKafkaOptions>(builder.Configuration.GetSection("Kafka"));
//builder.Services.AddHostedService<InventoryConsumer>();

var cs = builder.Configuration.GetConnectionString("Default") 
         ?? Environment.GetEnvironmentVariable("PA_INVENTORY_CS")
         ?? "server=localhost;port=3306;database=pa_mes_inventory;user=root;password=root";

builder.Services.AddDbContext<InventoryDbContext>(opt => opt.UseMySql(cs, ServerVersion.AutoDetect(cs)));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => { o.SwaggerDoc("v1", new OpenApiInfo { Title = "PA.Inventory.Api", Version = "v1" }); });
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "PA.Inventory.Api v1"); });
app.MapGrpcReflectionService();
app.MapHealthChecks("/healthz");
app.AddEndpoints();
app.Run();
