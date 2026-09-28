using System;
using System.Collections.Generic;
using ProductService.Domain.Enums;

namespace ProductService.Domain.Entities
{
    public class Product : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public string SKU { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public ProductStatus Status { get; set; } = ProductStatus.Active;

        public Guid CategoryId { get; set; }

        public Category? Category { get; set; }

        public Inventory? Inventory { get; set; }
    }
}

