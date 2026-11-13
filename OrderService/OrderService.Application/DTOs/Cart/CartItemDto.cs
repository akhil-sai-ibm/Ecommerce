using System;

namespace OrderService.Application.DTOs.Cart
{
    /// <summary>
    /// Item stored in a customer's shopping cart.
    /// </summary>
    public class CartItemDto
    {
        /// <summary>
        /// Unique cart item identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Product identifier.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Product display name.
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Product unit price.
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Quantity of product in cart.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Product image URL.
        /// </summary>
        public string? ProductImageUrl { get; set; }
    }
}
