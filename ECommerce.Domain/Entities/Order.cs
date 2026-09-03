using System;
using System.Collections.Generic;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities
{
    public class Order : BaseEntity
    {
        public Guid UserId { get; set; }

        public User? User { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public decimal TotalAmount { get; set; }

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

        public Payment? Payment { get; set; }
    }
}
