using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PA.Orders.Domain.Models
{
    public class Order
    {
        public long Id { get; set; }
        public long SiteId { get; set; }
        public long CustomerId { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, Paid, Shipped, Cancelled
        public decimal Total { get; set; }

        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? PaidOn { get; set; }
        public DateTimeOffset? ShippedOn { get; set; }

        public ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();
    }
}
