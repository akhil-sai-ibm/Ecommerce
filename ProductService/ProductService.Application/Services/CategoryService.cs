using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProductService.Application.DTOs.Categories;
using ProductService.Application.Interfaces.Repositories;
using ProductService.Application.Interfaces.Services;
using ProductService.Domain.Entities;
using ProductService.Domain.Exceptions;

namespace ProductService.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<CategoryDto?> GetByIdAsync(Guid id)
        {
            var c = await _categoryRepository.GetByIdAsync(id).ConfigureAwait(false);
            if (c == null) return null;
            return Map(c);
        }

        public async Task<IEnumerable<CategoryDto>> GetAllAsync()
        {
            var items = await _categoryRepository.GetAllAsync().ConfigureAwait(false);
            return items.Select(Map);
        }

        public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) throw new DomainException("Category name is required.");

            var exists = await _categoryRepository.GetByNameAsync(dto.Name.Trim()).ConfigureAwait(false);
            if (exists != null) throw new DomainException("Category with the same name already exists.");

            var category = new Category
            {
                Id = Guid.NewGuid(),
                Name = dto.Name.Trim(),
                Description = dto.Description ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            await _categoryRepository.AddAsync(category).ConfigureAwait(false);
            await _categoryRepository.SaveChangesAsync().ConfigureAwait(false);

            return Map(category);
        }

        public async Task UpdateAsync(Guid id, UpdateCategoryDto dto)
        {
            var category = await _categoryRepository.GetByIdAsync(id).ConfigureAwait(false);
            if (category == null) throw new DomainException("Category not found.");

            if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name.Trim() != category.Name)
            {
                var exists = await _categoryRepository.GetByNameAsync(dto.Name.Trim()).ConfigureAwait(false);
                if (exists != null && exists.Id != id) throw new DomainException("Category name already in use.");
                category.Name = dto.Name.Trim();
            }

            if (dto.Description != null) category.Description = dto.Description;
            category.UpdatedAt = DateTime.UtcNow;

            _categoryRepository.Update(category);
            await _categoryRepository.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task DeleteAsync(Guid id)
        {
            var category = await _categoryRepository.GetByIdAsync(id).ConfigureAwait(false);
            if (category == null) throw new DomainException("Category not found.");

            // Business rule: do not delete if products exist
            var withProducts = await _categoryRepository.GetWithProductsAsync(id).ConfigureAwait(false);
            if (withProducts != null && withProducts.Products.Any())
                throw new DomainException("Cannot delete category that has products.");

            _categoryRepository.Delete(category);
            await _categoryRepository.SaveChangesAsync().ConfigureAwait(false);
        }

        private static CategoryDto Map(Category c) => new CategoryDto { Id = c.Id, Name = c.Name, Description = c.Description };
    }
}

