using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaymentService.Domain.Entities
{
    public class Payment
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty; // Pending, Success, Failed
        public DateTime ProcessedAt { get; set; }
        public string? TransactionId { get; set; }
        
        [NotMapped]
        public int Method { get; set; }

        
    }
}
