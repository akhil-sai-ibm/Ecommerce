using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProductService.Domain.Entities;

namespace ProductService.Application.Interfaces.Repositories
{
    public interface IInventoryRepository : IGenericRepository<Inventory>
    {
        Task<Inventory?> GetByProductIdAsync(Guid productId);

        Task<IEnumerable<Inventory>> GetLowStockItemsAsync();

        Task UpdateQuantityAsync(Guid productId, int quantity);
    }
}

