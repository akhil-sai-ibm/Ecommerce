using System.ComponentModel.DataAnnotations;

namespace OrderService.Application.DTOs.Cart
{
    public class UpdateCartItemQuantityDto
    {
        [Range(1, 100)]
        public int Quantity { get; set; }
    }
}
