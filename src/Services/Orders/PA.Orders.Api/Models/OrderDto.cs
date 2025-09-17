using PA.Orders.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PA.Orders.Api.Models
{
    public class OrderDto
    {
        public long Id { get; set; }
        public long SiteId { get; set; }
        public long CustomerId { get; set; }

        public string Status { get; set; } = "Pending";
        public decimal Total { get; set; }

        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset? PaidOn { get; set; }
        public DateTimeOffset? ShippedOn { get; set; }

        public List<OrderLineDto> Lines { get; set; } = new();

        public OrderDto(Order model)
        {
            Id = model.Id;
            SiteId = model.SiteId;
            CustomerId = model.CustomerId;
            Status = model.Status;
            Total = model.Total;
            CreatedOn = model.CreatedOn;
            PaidOn = model.PaidOn;
            ShippedOn = model.ShippedOn;
            Lines = model.Lines.Select(l => new OrderLineDto(l)).ToList();
        }
    }
}
