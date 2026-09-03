using System;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.DTOs.Payments
{
    public class PaymentDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }
        public PaymentMethod Method { get; set; }
        public string? TransactionId { get; set; }
    }
}
