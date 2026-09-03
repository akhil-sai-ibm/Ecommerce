using System;
using System.Threading.Tasks;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;

namespace ECommerce.Infrastructure.Repositories
{
    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(ECommerceDbContext context) : base(context)
        {
        }

        public async Task<Category?> GetByNameAsync(string name)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(c => c.Name == name).ConfigureAwait(false);
        }

        public async Task<Category?> GetWithProductsAsync(Guid categoryId)
        {
            return await _dbSet.Include(c => c.Products).AsNoTracking().FirstOrDefaultAsync(c => c.Id == categoryId).ConfigureAwait(false);
        }
    }
}
