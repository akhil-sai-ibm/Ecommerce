using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;

namespace ECommerce.Infrastructure.Repositories
{
    public class InventoryRepository : GenericRepository<Inventory>, IInventoryRepository
    {
        public InventoryRepository(ECommerceDbContext context) : base(context)
        {
        }

        public async Task<Inventory?> GetByProductIdAsync(Guid productId)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(i => i.ProductId == productId).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Inventory>> GetLowStockItemsAsync()
        {
            return await _dbSet.Where(i => i.Quantity <= i.ReorderLevel).AsNoTracking().ToListAsync().ConfigureAwait(false);
        }

        public async Task UpdateQuantityAsync(Guid productId, int quantity)
        {
            if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity cannot be negative.");

            var entity = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == productId).ConfigureAwait(false);
            if (entity == null) throw new InvalidOperationException("Inventory record not found for product.");

            entity.Quantity = quantity;
            _context.Inventories.Update(entity);
        }
    }
}
