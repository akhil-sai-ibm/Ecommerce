using System;

namespace ECommerce.Domain.Entities
{
    /// <summary>
    /// Represents an idempotency record for checkout requests.
    /// Prevents duplicate orders when the same checkout request is retried.
    /// </summary>
    public class IdempotencyRecord : BaseEntity
    {
        /// <summary>
        /// Idempotency key provided by the client (e.g., UUID from request header).
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// The user who initiated the checkout.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The resulting order ID from the checkout, if successful.
        /// </summary>
        public Guid? OrderId { get; set; }

        /// <summary>
        /// Whether the checkout operation succeeded.
        /// </summary>
        public bool Succeeded { get; set; }

        /// <summary>
        /// Error message if the checkout failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// The response code/status (e.g., 200, 400, etc.).
        /// </summary>
        public int ResponseStatusCode { get; set; }

        /// <summary>
        /// HTTP method used (e.g., POST).
        /// </summary>
        public string Method { get; set; } = "POST";

        /// <summary>
        /// HTTP endpoint (e.g., /api/orders/checkout).
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;
    }
}
