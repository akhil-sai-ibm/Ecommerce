using System;

namespace PaymentService.Application.DTOs.Payments
{
    /// <summary>
    /// Details of a payment transaction or refund.
    /// </summary>
    public class PaymentDto
    {
        /// <summary>
        /// Unique payment identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Associated order identifier.
        /// </summary>
        public Guid OrderId { get; set; }

        /// <summary>
        /// Monetary payment amount.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Payment transaction status (0: Pending, 1: Success, 2: Failed, 3: RefundInProcess, 4: Refunded).
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Payment method (0: CreditCard, 1: DebitCard, 2: PayPal, etc.).
        /// </summary>
        public int Method { get; set; }

        /// <summary>
        /// External gateway transaction reference code.
        /// </summary>
        public string? TransactionId { get; set; }

        /// <summary>
        /// Timestamp when the payment was processed in UTC.
        /// </summary>
        public DateTime ProcessedAt { get; set; }
    }
}
