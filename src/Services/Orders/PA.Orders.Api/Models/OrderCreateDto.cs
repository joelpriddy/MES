namespace PA.Orders.Api.Models
{
    public class OrderCreateDto
    {
        public long SiteId { get; set; }
        public long CustomerId { get; set; }
        public List<OrderLineCreateDto> Lines { get; set; } = new();
    }
}
