using System;
using System.Collections.Generic;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities
{
    public class Product : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public string SKU { get; set; } = string.Empty;

        public ProductStatus Status { get; set; } = ProductStatus.Active;

        public Guid CategoryId { get; set; }

        public Category? Category { get; set; }

        public Inventory? Inventory { get; set; }

        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
