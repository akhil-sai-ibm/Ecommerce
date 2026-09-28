using System;
using System.Collections.Generic;

namespace ProductService.Application.DTOs.Inventory
{
    public class ReservationRequestDto
    {
        public Guid ReservationId { get; set; }
        public List<ReservationItemDto> Items { get; set; } = new();
    }
}
