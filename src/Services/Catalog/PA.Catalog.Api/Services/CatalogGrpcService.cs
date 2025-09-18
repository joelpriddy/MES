using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Catalog.V1;
using PA.Catalog.Data;

namespace PA.Catalog.Api.Services {
    public class CatalogGrpcService : CatalogService.CatalogServiceBase
    {
        private readonly CatalogDbContext _db;
        public CatalogGrpcService(CatalogDbContext db) => _db = db;

        public override async Task<GetProductResponse> GetProductBySku(GetProductBySkuRequest request, ServerCallContext context)
        {
            var entity = await _db.Set<PA.Catalog.Domain.Models.Product>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Sku == request.Sku, context.CancellationToken);

            var resp = new GetProductResponse();
            if (entity != null) {
                resp.Product = new Product {
                    Id = entity.Id,
                    Sku = entity.Sku,
                    Name = entity.Name,
                    Description = entity.Description ?? "",
                    IsManufactured = entity.IsManufactured,
                    Price = (double)entity.PriceRetail,
                    UnitOfMeasure = entity.UnitOfMeasure,
                    Active = entity.Active,
                    Subtype = entity.Subtype ?? "",
                    SizeLb = (double)(entity.SizeLb ?? 0)
                };
            }
            return resp;
        }
    }
}
