using Microsoft.EntityFrameworkCore;
using PA.Inventory.Api.Models;
using PA.Inventory.Data;
using PA.Inventory.Domain.Models;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Reflection;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(o =>
{
    o.ListenAnyIP(5081, lo => lo.Protocols = HttpProtocols.Http1AndHttp2);
});

//Services
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
// Config
var cs = builder.Configuration.GetConnectionString("Default") 
         ?? Environment.GetEnvironmentVariable("PA_INVENTORY_CS")
         ?? "server=localhost;port=3306;database=pa_mes_inventory;user=root;password=root";
builder.Services.AddDbContext<PA.Inventory.Data.InventoryDbContext>(opt =>
    opt.UseMySql(cs, ServerVersion.AutoDetect(cs)));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "PA.Inventory.Api", Version = "v1" });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PA.Inventory.Api v1");
});

app.MapGrpcReflectionService();
app.MapHealthChecks("/healthz");

//Endpoints
//GET
app.MapGet("/", () => $"Inventory service running ({Assembly.GetExecutingAssembly().GetName().Name}).");
app.MapGet("/stock", async (InventoryDbContext db) =>
{
    var list = await db.StockItems
        .AsNoTracking()
        .OrderBy(si => si.ProductId)
        .ThenBy(si => si.SiteId)
        .ThenBy(si => si.LotNumber)
        .Select(si => new StockItemDto(si))
        .ToListAsync();

    return Results.Ok(list);
});
app.MapGet("/stock/available/{productId:long}/{siteId:long}", async (InventoryDbContext db, long productId, long siteId) =>
{
    var available = await db.StockItems
        .AsNoTracking()
        .Where(x => x.ProductId == productId && x.SiteId == siteId)
        .Select(x => x.OnHand - x.Reserved)
        .SumAsync();

    return Results.Ok(new { productId, siteId, available });
})
.Produces(StatusCodes.Status200OK);

//POST
app.MapPost("/stock/intake", async (InventoryDbContext db, StockIntakeDto body) =>
{
    if (body.Quantity <= 0) { return Results.BadRequest("Quantity must be greater than zero."); }

    // Try to find an existing row for this (ProductId, SiteId, LotNumber)
    var entity = await db.StockItems
        .FirstOrDefaultAsync(x =>
            x.ProductId == body.ProductId &&
            x.SiteId == body.SiteId &&
            x.LotNumber == body.LotNumber);

    if (entity is null)
    {
        entity = new StockItem
        {
            ProductId = body.ProductId,
            SiteId = body.SiteId,
            LotNumber = body.LotNumber,
            Expiration = body.Expiration,
            OnHand = body.Quantity,
            Reserved = 0,
            UpdatedOn = DateTimeOffset.UtcNow
        };
        db.StockItems.Add(entity);
    }
    else
    {
        entity.OnHand += body.Quantity;
        if (body.Expiration.HasValue) { entity.Expiration = body.Expiration; }
        entity.UpdatedOn = DateTimeOffset.UtcNow;
    }

    await db.SaveChangesAsync();
    return Results.Ok(new StockItemDto(entity));
})
.Produces<StockItemDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

app.MapPost("/stock/consume", async (InventoryDbContext db, StockConsumeDto body) =>
{
    if (body.Quantity <= 0) { return Results.BadRequest("Quantity must be greater than zero."); }

    // Start by narrowing by product/site
    IQueryable<StockItem> q = db.StockItems
        .Where(x => x.ProductId == body.ProductId && x.SiteId == body.SiteId);

    StockItem? entity;

    if (!string.IsNullOrWhiteSpace(body.LotNumber))
    {
        // Consume from the specified lot
        entity = await q.FirstOrDefaultAsync(x => x.LotNumber == body.LotNumber);
        if (entity is null) { return Results.NotFound($"No stock row for ProductId={body.ProductId}, SiteId={body.SiteId}, Lot={body.LotNumber}."); }
    }
    else
    {
        // No lot specified: choose earliest-expiring lot (nulls last), then oldest updated
        entity = await q
            .OrderBy(x => x.Expiration.HasValue ? 0 : 1)
            .ThenBy(x => x.Expiration)
            .ThenBy(x => x.UpdatedOn)
            .FirstOrDefaultAsync();

        if (entity is null) { return Results.NotFound($"No stock rows for ProductId={body.ProductId}, SiteId={body.SiteId}."); }
    }

    var available = entity.OnHand - entity.Reserved;
    if (available < body.Quantity)
    {
        return Results.BadRequest($"Insufficient available quantity. Available: {available}, Requested: {body.Quantity}.");
    }

    entity.OnHand -= body.Quantity;
    entity.UpdatedOn = DateTimeOffset.UtcNow;

    await db.SaveChangesAsync();

    return Results.Ok(new StockItemDto(entity));
})
.Produces<StockItemDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status404NotFound);

app.Run();
