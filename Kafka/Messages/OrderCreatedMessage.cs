using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messages
{

    public class OrderItemUnit
    {
        public long Id { get; set; }

        public long OrderId { get; set; }

        public long ProductId { get; set; }

        public int Quantity { get; set; }

        public string ProductTitle { get; set; }

        public string ProductUrl { get; set; }

        public long PriceCents { get; set; }

        public string PriceCurrency { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }
    }
    public class OrderCreatedMessage
    {
        public long Id { get; set; }

        public long CustomerId { get; set; }

        public string DeliveryAddress { get; set; }

        public long TotalPriceCents { get; set; }

        public string TotalPriceCurrency { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public OrderItemUnit[] OrderItems { get; set; }
    }
}
