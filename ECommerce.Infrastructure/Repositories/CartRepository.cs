using System;
using System.Threading.Tasks;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;

namespace ECommerce.Infrastructure.Repositories
{
    public class CartRepository : GenericRepository<Cart>, ICartRepository
    {
        public CartRepository(ECommerceDbContext context) : base(context)
        {
        }

        public async Task<Cart?> GetByUserIdAsync(Guid userId)
        {
            return await _dbSet
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId)
                .ConfigureAwait(false);
        }

        public async Task<Cart?> GetWithItemsAsync(Guid cartId)
        {
            return await _dbSet.Include(c => c.Items)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.Id == cartId).ConfigureAwait(false);
        }

        public async Task RemoveCartItemsByProductIdAsync(Guid productId)
        {
            var cartItems = await _context.Set<CartItem>().Where(ci => ci.ProductId == productId).ToListAsync().ConfigureAwait(false);
            if (cartItems.Any())
            {
                _context.Set<CartItem>().RemoveRange(cartItems);
            }
        }
    }
}
