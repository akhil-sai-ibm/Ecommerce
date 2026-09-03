using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.DTOs.Categories;

namespace ECommerce.Application.Interfaces.Services
{
    public interface ICategoryService
    {
        Task<CategoryDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<CategoryDto>> GetAllAsync();
        Task<CategoryDto> CreateAsync(CreateCategoryDto dto);
        Task UpdateAsync(Guid id, UpdateCategoryDto dto);
        Task DeleteAsync(Guid id);
    }
}
