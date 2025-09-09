using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Config
var cs = builder.Configuration.GetConnectionString("Default") 
         ?? Environment.GetEnvironmentVariable("PA_INVENTORY_CS")
         ?? "server=localhost;port=3306;database=pa_mes_inventory;user=root;password=root";

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
builder.Services.AddHealthChecks();
builder.Services.AddDbContext<PA.Inventory.Data.InventoryDbContext>(opt =>
    opt.UseMySql(cs, ServerVersion.AutoDetect(cs)));

var app = builder.Build();

app.MapGrpcReflectionService();
app.MapHealthChecks("/healthz");
app.MapGet("/", () => $"Inventory service running ({Assembly.GetExecutingAssembly().GetName().Name}).");

app.Run();
