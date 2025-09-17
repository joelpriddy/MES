using System.Reflection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PA.Orders.Api.Infrastructure.Kafka;
using PA.Orders.Api.Endpoints;
using System;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
var httpPortEnv = Environment.GetEnvironmentVariable("HTTP_PORT");
var httpPort = 5082;

if (int.TryParse(httpPortEnv, out var parsed)) { httpPort = parsed; }

builder.WebHost.ConfigureKestrel(o =>
{
    o.ListenAnyIP(httpPort, lo => { lo.Protocols = HttpProtocols.Http1AndHttp2; });
});


// Config
var cs = builder.Configuration.GetConnectionString("Default") 
         ?? Environment.GetEnvironmentVariable("PA_ORDERS_CS")
         ?? "server=localhost;port=3306;database=pa_mes_orders;user=root;password=root";
builder.Services.Configure<OrdersKafkaOptions>(builder.Configuration.GetSection("Kafka"));
builder.Services.AddSingleton<IOrderEventPublisher, OrderEventPublisher>();
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
builder.Services.AddHealthChecks(); 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => { o.SwaggerDoc("v1", new OpenApiInfo { Title = "PA.Orders.Api", Version = "v1" }); });
builder.Services.AddDbContext<PA.Orders.Data.OrdersDbContext>(opt => opt.UseMySql(cs, ServerVersion.AutoDetect(cs)));

var app = builder.Build();

//App setup
app.AddEndpoints();
app.UseSwagger();
app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "PA.Orders.Api v1"); });
app.MapGrpcReflectionService();
app.MapHealthChecks("/healthz");
app.MapGet("/", () => $"Orders service running ({Assembly.GetExecutingAssembly().GetName().Name}).");
app.Run();
