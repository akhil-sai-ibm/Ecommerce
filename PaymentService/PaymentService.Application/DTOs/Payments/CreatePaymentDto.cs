using System;
using System.ComponentModel.DataAnnotations;

namespace PaymentService.Application.DTOs.Payments
{
    /// <summary>
    /// Request payload for creating a new payment against an existing order.
    /// </summary>
    public class CreatePaymentDto
    {
        /// <summary>
        /// Associated order identifier to pay.
        /// </summary>
        [Required]
        public Guid OrderId { get; set; }

        /// <summary>
        /// Monetary amount to be charged.
        /// </summary>
        [Required]
        [Range(0.01, 1000000.00)]
        public decimal Amount { get; set; }

        /// <summary>
        /// Payment method code (0: CreditCard, 1: DebitCard, 2: PayPal).
        /// </summary>
        [Required]
        public int Method { get; set; }
    }
}
