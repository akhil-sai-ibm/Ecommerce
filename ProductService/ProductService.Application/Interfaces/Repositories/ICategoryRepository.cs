using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using ProductService.Domain.Entities;

namespace ProductService.Application.Interfaces.Repositories
{
    public interface ICategoryRepository : IGenericRepository<Category>
    {
        Task<Category?> GetByNameAsync(string name);

        Task<Category?> GetWithProductsAsync(Guid categoryId);
    }
}

