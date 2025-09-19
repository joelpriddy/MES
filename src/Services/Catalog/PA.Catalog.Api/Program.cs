using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PA.Catalog.Api.Endpoints;
using PA.Catalog.Api.Models;
using PA.Catalog.Api.Services;
using PA.Catalog.Data;
using PA.Catalog.Data.Seed;
using PA.Catalog.Domain.Models;

var builder = WebApplication.CreateBuilder(args);
var portStr = Environment.GetEnvironmentVariable("HTTP_PORT")
    ?? builder.Configuration["HTTP_PORT"]
    ?? "8080";

if (int.TryParse(portStr, out var httpPort))
{
    builder.WebHost.ConfigureKestrel(o =>
    {
        o.ListenAnyIP(httpPort, lo => lo.Protocols = HttpProtocols.Http1AndHttp2);
    });
}

// CORS
builder.Services.AddCors(o => {
    o.AddPolicy("allow_local", p => p
        .WithOrigins("http://localhost:5173", "http://localhost:5174") // your React dev origins
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// Services
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

var cs = builder.Configuration.GetConnectionString("Default")
         ?? "server=localhost;port=3306;database=pa_mes_catalog;user=root;password=root";
builder.Services.AddDbContext<CatalogDbContext>(opt =>
    opt.UseMySql(cs, ServerVersion.AutoDetect(cs)));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "PA.Catalog.Api", Version = "v1" });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

// Ensure DB is up-to-date and seed initial products
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

    db.Database.Migrate();
    CatalogSeeder.SeedAsync(db).GetAwaiter().GetResult();
}

//CORS
app.UseCors("allow_local");
// Swagger middleware
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PA.Catalog.Api v1");
});

// gRPC endpoints (reflection + service)
app.MapGrpcReflectionService();
app.MapHealthChecks("/healthz");
app.MapGrpcService<CatalogGrpcService>();
app.AddEndpoints();
app.Run();
