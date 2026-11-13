using System;
using System.Collections.Generic;

namespace OrderService.Application.DTOs.Cart
{
    /// <summary>
    /// Represents a customer's active shopping cart and its contents.
    /// </summary>
    public class CartDto
    {
        /// <summary>
        /// Unique cart identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Owner customer identifier.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Collection of items currently stored in the cart.
        /// </summary>
        public IEnumerable<CartItemDto> Items { get; set; } = new List<CartItemDto>();
    }
}
