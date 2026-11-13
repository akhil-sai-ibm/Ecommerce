using System;
using System.ComponentModel.DataAnnotations;

namespace OrderService.Application.DTOs.Cart
{
    /// <summary>
    /// Request payload for adding an item to the shopping cart.
    /// </summary>
    public class AddToCartDto
    {
        /// <summary>
        /// Product identifier to add.
        /// </summary>
        [Required]
        public Guid ProductId { get; set; }
        
        /// <summary>
        /// Desired quantity to add (1 to 100).
        /// </summary>
        [Range(1, 100)]
        public int Quantity { get; set; }
    }
}
