using System.Reflection;
using Microsoft.EntityFrameworkCore;
using PA.Catalog.Api.Models;
using PA.Catalog.Data;
using PA.Catalog.Domain.Models;

namespace PA.Catalog.Api.Endpoints
{
    public static class EndpointMapping
    {
        public static WebApplication AddEndpoints(this WebApplication app)
        {
            app.AddGets()
                .AddPosts()
                .AddPuts()
                .AddDeletes();

            return app;
        }

        private static WebApplication AddGets(this WebApplication app)
        {
            app.MapGet("/", () => "Catalog service running (PA.Catalog.Api).");

            app.MapGet("/products", async (CatalogDbContext db) =>
                await db.Products
                    .AsNoTracking()
                    .Select(p => new ProductDto(p))
                    .ToListAsync());

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

            return app;
        }

        private static WebApplication AddPosts(this WebApplication app)
        {
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
                    Subtype = body.Subtype,
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

            return app;
        }

        private static WebApplication AddPuts(this WebApplication app)
        {
            app.MapPut("/products/{id:long}", async (CatalogDbContext db, long id, ProductUpdateDto body) =>
            {
                var entity = await db.Products.FirstOrDefaultAsync(p => p.Id == id);

                if (entity is null) { return Results.NotFound(); }

                if (body.Name is not null) { entity.Name = body.Name; }
                if (body.Description is not null) { entity.Description = body.Description; }
                if (body.IsManufactured.HasValue) { entity.IsManufactured = body.IsManufactured.Value; }
                if (body.UnitOfMeasure is not null) { entity.UnitOfMeasure = body.UnitOfMeasure; }
                if (body.Subtype is not null) { entity.Subtype = body.Subtype; }
                if (body.SizeLb.HasValue) { entity.SizeLb = body.SizeLb; }
                if (body.PriceRetail.HasValue) { entity.PriceRetail = body.PriceRetail.Value; }
                if (body.PriceWholesale.HasValue) { entity.PriceWholesale = body.PriceWholesale; }
                if (body.Active.HasValue) { entity.Active = body.Active.Value; }

                await db.SaveChangesAsync();

                return Results.Ok(new ProductDto(entity));
            })
            .Produces<ProductDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

            return app;
        }

        private static WebApplication AddDeletes(this WebApplication app)
        {
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

            return app;
        }
    }
}
