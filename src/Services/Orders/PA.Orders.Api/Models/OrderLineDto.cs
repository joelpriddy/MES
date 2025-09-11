using PA.Orders.Domain.Models;

namespace PA.Orders.Api.Models
{
    public class OrderLineDto
    {
        public long Id { get; set; }
        public long ProductId { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public OrderLineDto(OrderLine model)
        {
            Id = model.Id;
            ProductId = model.ProductId;
            Quantity = model.Quantity;
            UnitPrice = model.UnitPrice;
        }
    }
}

