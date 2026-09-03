using System;

namespace ECommerce.Application.DTOs.Inventory
{
    public class UpdateInventoryDto
    {
        public int? Quantity { get; set; }
        public int? ReorderLevel { get; set; }
    }
}
