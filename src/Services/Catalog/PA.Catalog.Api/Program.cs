using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PA.Catalog.Api.Models;
using PA.Catalog.Api.Services;
using PA.Catalog.Data;
using PA.Catalog.Domain.Models;

var builder = WebApplication.CreateBuilder(args);

// Kestrel: HTTP/1.1 + HTTP/2 on port 5080
builder.WebHost.ConfigureKestrel(o =>
{
    o.ListenAnyIP(5080, lo => lo.Protocols = HttpProtocols.Http1AndHttp2);
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

// Swagger middleware
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PA.Catalog.Api v1");
});

// gRPC endpoints (reflection + service)
app.MapGrpcReflectionService();
app.MapHealthChecks("/healthz");
// app.MapGrpcService<CatalogGrpcService>(); // uncomment when service is ready

// Minimal APIs
app.MapGet("/", () => "Catalog service running (PA.Catalog.Api).");

// GET /products (uses ProductDto you added earlier)
app.MapGet("/products", async (CatalogDbContext db) =>
    await db.Products
        .AsNoTracking()
        .Select(p => new ProductDto(p))
        .ToListAsync());

// GET /products/{id}
app.MapGet("/products/{id:long}", async (CatalogDbContext db, long id) =>
{
    var p = await db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

    if (p is null) { return Results.NotFound(); }

    var dto = new ProductDto(p);

    return Results.Ok(dto);
})
.Produces<ProductDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapGet("/products/by-sku/{sku}", async (CatalogDbContext db, string sku) =>
{
    var p = await db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Sku == sku);
    if (p is null) { return Results.NotFound(); }

    return Results.Ok(new ProductDto(p));
})
.Produces<ProductDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

// POST /products (uses ProductCreateDto)
app.MapPost("/products", async (CatalogDbContext db, ProductCreateDto body) =>
{    
    var exists = await db.Products.AnyAsync(p => p.Sku == body.Sku);// basic SKU uniqueness check

    if (exists) { return Results.Conflict($"SKU '{body.Sku}' already exists."); }

    var entity = new Product
    {
        Sku = body.Sku,
        Name = body.Name,
        Description = body.Description,
        IsManufactured = body.IsManufactured,
        UnitOfMeasure = body.UnitOfMeasure,
        Flavor = body.Flavor,
        SizeLb = body.SizeLb,
        PriceRetail = body.PriceRetail,
        PriceWholesale = body.PriceWholesale,
        Active = body.Active
    };

    db.Products.Add(entity);

    await db.SaveChangesAsync();

    var dto = new ProductDto(entity);

    return Results.Created($"/products/{entity.Id}", dto);
})
.Produces<ProductDto>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status409Conflict);

// PUT /products/{id} (uses ProductUpdateDto)
app.MapPut("/products/{id:long}", async (CatalogDbContext db, long id, ProductUpdateDto body) =>
{
    var entity = await db.Products.FirstOrDefaultAsync(p => p.Id == id);

    if (entity is null) { return Results.NotFound(); }

    if (body.Name is not null) { entity.Name = body.Name; }
    if (body.Description is not null) { entity.Description = body.Description; }
    if (body.IsManufactured.HasValue) { entity.IsManufactured = body.IsManufactured.Value; }
    if (body.UnitOfMeasure is not null) { entity.UnitOfMeasure = body.UnitOfMeasure; }
    if (body.Flavor is not null) { entity.Flavor = body.Flavor; }
    if (body.SizeLb.HasValue) { entity.SizeLb = body.SizeLb; }
    if (body.PriceRetail.HasValue) { entity.PriceRetail = body.PriceRetail.Value; }
    if (body.PriceWholesale.HasValue) { entity.PriceWholesale = body.PriceWholesale; }
    if (body.Active.HasValue) { entity.Active = body.Active.Value; }

    await db.SaveChangesAsync();

    return Results.Ok(new ProductDto(entity));
})
.Produces<ProductDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapDelete("/products/{id:long}", async (PA.Catalog.Data.CatalogDbContext db, long id) =>
{
    var entity = await db.Products.FirstOrDefaultAsync(p => p.Id == id);
    if (entity is null) { return Results.NotFound(); }

    db.Products.Remove(entity);
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

app.Run();
