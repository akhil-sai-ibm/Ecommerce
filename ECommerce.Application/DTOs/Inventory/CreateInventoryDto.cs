using System;

namespace ECommerce.Application.DTOs.Inventory
{
    public class CreateInventoryDto
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public int ReorderLevel { get; set; }
    }
}
