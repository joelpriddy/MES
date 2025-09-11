using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PA.Orders.Domain.Models
{
    public class OrderLine
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public long ProductId { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
