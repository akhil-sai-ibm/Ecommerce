using System;
using System.Collections.Generic;

namespace OrderService.Application.DTOs.Cart
{
    public class CartDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public IEnumerable<CartItemDto> Items { get; set; } = new List<CartItemDto>();
    }
}
