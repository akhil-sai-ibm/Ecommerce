using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrderService.Application.Interfaces.Clients
{
    public interface IProductServiceClient
    {
        Task<bool> ReserveInventoryAsync(Guid reservationId, IEnumerable<(Guid ProductId, int Quantity)> items);
        Task ReleaseInventoryAsync(Guid reservationId);
        Task ConfirmInventoryAsync(Guid reservationId);
        Task<IEnumerable<ProductSnapshot>> GetBulkProductsAsync(IEnumerable<Guid> productIds);
        Task<int> GetProductStockAsync(Guid productId);
    }

    public class ProductSnapshot
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
    }
}
