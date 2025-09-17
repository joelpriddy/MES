using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using PA.Orders.Api.Infrastructure.Kafka;
using PA.Orders.Api.Models;
using PA.Orders.Data;
using PA.Orders.Domain.Models;
using Microsoft.AspNetCore.Builder; // WebApplication
using Microsoft.AspNetCore.Http;    // Results
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace PA.Orders.Api.Endpoints
{
    public static class EndpointMapping
    {
        public static WebApplication AddEndpoints(this WebApplication app)
        {
            app.AddGets()
                .AddPosts()
                .AddPuts();   

            return app;
        }

        private static WebApplication AddGets(this WebApplication app)
        {
            app.MapGet("/orders", async (OrdersDbContext db) =>
            {
                var list = await db.Orders
                    .AsNoTracking()
                    .Include(o => o.Lines)
                    .Select(o => new OrderDto(o))
                    .ToListAsync();

                return Results.Ok(list);
            });

            app.MapGet("/orders/{id:long}", async (OrdersDbContext db, long id) =>
            {
                var order = await db.Orders
                    .AsNoTracking()
                    .Include(o => o.Lines)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order is null) { return Results.NotFound(); }

                return Results.Ok(new OrderDto(order));
            })
            .Produces<OrderDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

            return app;
        }

        private static WebApplication AddPosts(this WebApplication app)
        {
            app.MapPost("/orders", async (OrdersDbContext db, OrderCreateDto body, IOrderEventPublisher publisher) =>
            {
                if (body is null || body.Lines is null || body.Lines.Count == 0)
                {
                    return Results.BadRequest("Order must contain at least one line.");
                }

                if (body.Lines.Any(l => l.Quantity <= 0 || l.UnitPrice < 0))
                {
                    return Results.BadRequest("Each line must have Quantity > 0 and UnitPrice >= 0.");
                }

                var order = new Order
                {
                    SiteId = body.SiteId,
                    CustomerId = body.CustomerId,
                    Status = "Pending",
                    CreatedOn = DateTimeOffset.UtcNow
                };

                foreach (var l in body.Lines)
                {
                    order.Lines.Add(new OrderLine
                    {
                        ProductId = l.ProductId,
                        Quantity = l.Quantity,
                        UnitPrice = l.UnitPrice
                    });
                }

                order.Total = order.Lines.Sum(x => x.Quantity * x.UnitPrice);

                db.Orders.Add(order);
                await db.SaveChangesAsync();

                await publisher.PublishOrderCreatedAsync(order);

                return Results.Created($"/orders/{order.Id}", new OrderDto(order));
            })
            .Produces<OrderDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

            return app;
        }

        private static WebApplication AddPuts(this WebApplication app)
        {
            app.MapPut("/orders/{id:long}/ship", async (OrdersDbContext db, long id, IOrderEventPublisher publisher) =>
            {
                var order = await db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id);

                if (order is null) { return Results.NotFound(); }

                order.Status = "Shipped";
                order.ShippedOn = DateTimeOffset.UtcNow;

                await db.SaveChangesAsync();
                await publisher.PublishOrderShippedAsync(order);

                return Results.Ok(new OrderDto(order));
            })
            .Produces<OrderDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

            app.MapPut("/orders/{id:long}/pay", async (OrdersDbContext db, long id) =>
            {
                var order = await db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id);

                if (order is null) { return Results.NotFound(); }

                if (order.Status == "Paid" || order.Status == "Shipped")
                {
                    return Results.Conflict($"Order {id} is already {order.Status}.");
                }

                order.Status = "Paid";
                order.PaidOn = DateTimeOffset.UtcNow;

                await db.SaveChangesAsync();

                return Results.Ok(new OrderDto(order));
            })
            .Produces<OrderDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

            app.MapPut("/orders/{id:long}/cancel", async (OrdersDbContext db, long id, IOrderEventPublisher publisher) =>
            {
                var order = await db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id);

                if (order is null) { return Results.NotFound(); }
                if (order.Status is "Shipped") { return Results.Conflict($"Order {id} is already Shipped and cannot be canceled."); }

                order.Status = "Canceled";

                await db.SaveChangesAsync();
                await publisher.PublishOrderCanceledAsync(order);

                return Results.Ok(new OrderDto(order));
            })
            .Produces<OrderDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

            return app;
        }
    }
}
