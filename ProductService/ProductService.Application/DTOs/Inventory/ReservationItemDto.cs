using System;
using System.Collections.Generic;

namespace ProductService.Application.DTOs.Inventory
{
    public class ReservationItemDto
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
