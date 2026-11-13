using System;

namespace OrderService.Application.DTOs.Order
{
    /// <summary>
    /// Line item representing a purchased product within an order.
    /// </summary>
    public class OrderItemDto
    {
        /// <summary>
        /// Unique order item identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Product identifier.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Snapshot of product name at time of order creation.
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Snapshot of product image URL.
        /// </summary>
        public string? ProductImageUrl { get; set; }

        /// <summary>
        /// Purchased quantity.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Unit price charged per item.
        /// </summary>
        public decimal UnitPrice { get; set; }
    }
}
