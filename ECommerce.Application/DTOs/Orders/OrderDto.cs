using System;
using System.Collections.Generic;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.DTOs.Orders
{
    public class OrderDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public OrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
        public Guid? PaymentId { get; set; }
    }
}
