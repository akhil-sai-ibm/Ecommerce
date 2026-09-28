using System;

namespace ProductService.Application.DTOs.Products
{
    public class ProductDeleteResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsSoftDeleted { get; set; }
    }
}

