using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProductService.Application.DTOs.Products;
using ProductService.Application.DTOs.Categories;

namespace ProductService.Application.Interfaces.Services
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

