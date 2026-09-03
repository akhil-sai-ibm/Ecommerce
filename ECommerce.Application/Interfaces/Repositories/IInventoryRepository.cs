using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Interfaces.Repositories
{
    public interface IInventoryRepository : IGenericRepository<Inventory>
    {
        Task<Inventory?> GetByProductIdAsync(Guid productId);

        Task<IEnumerable<Inventory>> GetLowStockItemsAsync();

        Task UpdateQuantityAsync(Guid productId, int quantity);
    }
}
