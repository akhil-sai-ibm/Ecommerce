using System;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities
{
    public class Payment : BaseEntity
    {
        public Guid OrderId { get; set; }

        public Order? Order { get; set; }

        public decimal Amount { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public PaymentMethod Method { get; set; } = PaymentMethod.CreditCard;

        public string? TransactionId { get; set; }
    }
}
