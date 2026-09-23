using System;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.DTOs.Products
{
    public class CreateProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public ProductStatus Status { get; set; } = ProductStatus.Active;
        public Guid CategoryId { get; set; }
    }
}
