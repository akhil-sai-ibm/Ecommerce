using System;

namespace OrderService.Domain.Entities
{
    public class Payment
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty; // Success, Failed
        public DateTime ProcessedAt { get; set; }
        public string? TransactionId { get; set; }
        
        public Order Order { get; set; }
    }
}
