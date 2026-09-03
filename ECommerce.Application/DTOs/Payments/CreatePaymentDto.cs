using System;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.DTOs.Payments
{
    public class CreatePaymentDto
    {
        public Guid OrderId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public string? TransactionId { get; set; }
    }
}
