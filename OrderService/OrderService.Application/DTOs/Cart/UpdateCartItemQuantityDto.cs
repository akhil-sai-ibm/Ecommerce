using System.ComponentModel.DataAnnotations;

namespace OrderService.Application.DTOs.Cart
{
    /// <summary>
    /// Request payload for updating the quantity of an item in the cart.
    /// </summary>
    public class UpdateCartItemQuantityDto
    {
        /// <summary>
        /// New item quantity (must be between 1 and 100).
        /// </summary>
        [Range(1, 100)]
        public int Quantity { get; set; }
    }
}
