using System.Reflection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using PA.Orders.Api.Models;
using PA.Orders.Data;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(o =>
{
    o.ListenAnyIP(5082, lo => lo.Protocols = HttpProtocols.Http1AndHttp2);
});

// Config
var cs = builder.Configuration.GetConnectionString("Default") 
         ?? Environment.GetEnvironmentVariable("PA_ORDERS_CS")
         ?? "server=localhost;port=3306;database=pa_mes_orders;user=root;password=root";

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
builder.Services.AddHealthChecks(); 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "PA.Orders.Api", Version = "v1" });
});

builder.Services.AddDbContext<PA.Orders.Data.OrdersDbContext>(opt =>
    opt.UseMySql(cs, ServerVersion.AutoDetect(cs)));

var app = builder.Build();

app.MapGet("/orders", async (OrdersDbContext db) =>
{
    var list = await db.Orders
        .AsNoTracking()
        .Include(o => o.Lines)
        .Select(o => new OrderDto(o))
        .ToListAsync();

    return Results.Ok(list);
});

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PA.Orders.Api v1");
});
app.MapGrpcReflectionService();
app.MapHealthChecks("/healthz");
app.MapGet("/", () => $"Orders service running ({Assembly.GetExecutingAssembly().GetName().Name}).");

app.Run();
