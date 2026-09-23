using System;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.DTOs.Orders
{
    public class OrderNotificationDto
    {
        public Guid OrderId { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
