using System;

namespace PaymentService.Application.DTOs.Payments
{
    public class CreatePaymentDto
    {
        public Guid OrderId { get; set; }
        public decimal Amount { get; set; }
        public int Method { get; set; }
    }
}
