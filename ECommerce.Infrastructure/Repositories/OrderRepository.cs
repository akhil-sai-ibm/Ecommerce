using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;

namespace ECommerce.Infrastructure.Repositories
{
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        public OrderRepository(ECommerceDbContext context) : base(context)
        {
        }

        public async Task<Order?> GetByIdWithDetailsAsync(Guid orderId)
        {
            return await _dbSet
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Payment)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId)
                .ConfigureAwait(false);
        }

        public async Task<IEnumerable<Order>> GetByUserIdAsync(Guid userId)
        {
            return await _dbSet.Where(o => o.UserId == userId).AsNoTracking().ToListAsync().ConfigureAwait(false);
        }

        public async Task<IEnumerable<Order>> GetAllWithDetailsAsync()
        {
            return await _dbSet
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Payment)
                .AsNoTracking()
                .ToListAsync()
                .ConfigureAwait(false);
        }

        public async Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status)
        {
            return await _dbSet.Where(o => o.Status == status).AsNoTracking().ToListAsync().ConfigureAwait(false);
        }

        public async Task<bool> HasOrderItemsAsync(Guid productId)
        {
            return await _context.Set<OrderItem>().AnyAsync(oi => oi.ProductId == productId).ConfigureAwait(false);
        }
    }
}
