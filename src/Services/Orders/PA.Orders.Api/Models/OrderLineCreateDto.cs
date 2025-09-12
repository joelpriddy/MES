namespace PA.Orders.Api.Models
{
    public class OrderLineCreateDto
    {
        public long ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }   // for now we accept price from caller
    }
}
