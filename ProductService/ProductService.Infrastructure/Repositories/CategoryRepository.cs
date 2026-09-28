using System;
using System.Threading.Tasks;
using ProductService.Application.Interfaces.Repositories;
using ProductService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ProductService.Infrastructure.Data;

namespace ProductService.Infrastructure.Repositories
{
    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(ProductServiceDbContext context) : base(context)
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


