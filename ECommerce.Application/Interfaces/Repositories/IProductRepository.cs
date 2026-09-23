using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Interfaces.Repositories
{
    public interface IProductRepository : IGenericRepository<Product>
    {
        Task<Product?> GetBySkuAsync(string sku);

        Task<IEnumerable<Product>> GetByCategoryAsync(Guid categoryId);

        Task<(IEnumerable<Product> Items, int TotalCount)> SearchAsync(string? searchTerm, Guid? categoryId, int pageNumber, int pageSize, string? sortBy, string? sortDirection, ProductStatus? status = null);

        Task<IEnumerable<Product>> GetActiveProductsAsync();
    }
}
