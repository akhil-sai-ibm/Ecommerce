namespace ECommerce.Application.DTOs.Products
{
    public class BulkProductErrorDto
    {
        public int RowNumber { get; set; }
        public string? Sku { get; set; }
        public string Error { get; set; } = string.Empty;
    }
}
