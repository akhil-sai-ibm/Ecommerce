using System;

namespace PaymentService.Application.DTOs.Payments
{
    public class PaymentDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public decimal Amount { get; set; }
        public int Status { get; set; }
        public int Method { get; set; }
        public string? TransactionId { get; set; }
        public DateTime ProcessedAt { get; set; }
    }
}
