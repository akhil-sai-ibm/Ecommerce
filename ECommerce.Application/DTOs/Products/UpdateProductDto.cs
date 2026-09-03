using System;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.DTOs.Products
{
    public class UpdateProductDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public string? SKU { get; set; }
        public ProductStatus? Status { get; set; }
        public Guid? CategoryId { get; set; }
    }
}
