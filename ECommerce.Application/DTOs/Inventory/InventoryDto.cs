using System;

namespace ECommerce.Application.DTOs.Inventory
{
    public class InventoryDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public int ReorderLevel { get; set; }
    }
}
