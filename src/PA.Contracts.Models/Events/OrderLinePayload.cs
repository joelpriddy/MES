using System;
using System.Collections.Generic;

namespace PA.Contracts.Models.Events
{
    public class OrderLinePayload
    {
        public long ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
