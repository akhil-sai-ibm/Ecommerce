using System;
using System.Collections.Generic;

namespace ProductService.Application.DTOs.Inventory
{
    public class ReservationResultDto
    {
        public bool Success { get; set; }
        public List<string> MissingItems { get; set; } = new();
        public string Message { get; set; }
    }
}
