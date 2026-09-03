using System;
using System.Threading.Tasks;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;

namespace ECommerce.Infrastructure.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(ECommerceDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbSet.Include(u => u.Role).AsNoTracking().FirstOrDefaultAsync(u => u.Email == email).ConfigureAwait(false);
        }

        public async Task<User?> GetWithOrdersAsync(Guid userId)
        {
            return await _dbSet.Include(u => u.Orders)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId).ConfigureAwait(false);
        }

        public async Task<User?> GetWithCartAsync(Guid userId)
        {
            return await _dbSet.Include(u => u.Cart)
                .ThenInclude(c => c.Items)
                .ThenInclude(ci => ci.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId).ConfigureAwait(false);
        }
    }
}
