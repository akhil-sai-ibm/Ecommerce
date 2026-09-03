using System;
using System.Collections.Generic;

namespace ECommerce.Application.DTOs.Cart
{
    public class CartDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public List<CartItemDto> Items { get; set; } = new List<CartItemDto>();
        public decimal Total { get; set; }
    }
}
