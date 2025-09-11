using PA.Inventory.Domain.Models;

namespace PA.Inventory.Api.Models
{
    public class StockItemDto
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public long SiteId { get; set; }

        public string? LotNumber { get; set; }
        public DateTimeOffset? Expiration { get; set; }

        public decimal OnHand { get; set; }
        public decimal Reserved { get; set; }

        public DateTimeOffset UpdatedOn { get; set; }

        public StockItemDto(StockItem model)
        {
            Id = model.Id;
            ProductId = model.ProductId;
            SiteId = model.SiteId;
            LotNumber = model.LotNumber;
            Expiration = model.Expiration;
            OnHand = model.OnHand;
            Reserved = model.Reserved;
            UpdatedOn = model.UpdatedOn;
        }
    }
}
