using System;
using System.ComponentModel.DataAnnotations;

namespace OrderService.Application.DTOs.Cart
{
    public class AddToCartDto
    {
        [Required]
        public Guid ProductId { get; set; }
        
        [Range(1, 100)]
        public int Quantity { get; set; }
    }
}
