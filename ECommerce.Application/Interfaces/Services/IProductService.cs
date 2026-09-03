using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Application.DTOs.Products;

namespace ECommerce.Application.Interfaces.Services
{
    public interface IProductService
    {
        Task<ProductDto?> GetByIdAsync(Guid id);

        Task<IEnumerable<ProductDto>> GetAllAsync();

        Task<(IEnumerable<ProductDto> Items, int TotalCount)> SearchAsync(string? searchTerm, Guid? categoryId, int pageNumber, int pageSize, string? sortBy, string? sortDirection);

        Task<IEnumerable<ProductDto>> GetByCategoryAsync(Guid categoryId);

        Task<ProductDto> CreateAsync(CreateProductDto dto);

        Task UpdateAsync(Guid id, UpdateProductDto dto);

        Task DeleteAsync(Guid id);
    }
}
