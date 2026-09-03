using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IInventoryRepository _inventoryRepository;
        private readonly ICacheService _cacheService;
        private readonly ILogger<ProductService> _logger;

        private const string ProductCacheKeyPrefix = "product_";
        private const string AllProductsCacheKey = "all_products";
        private const string SearchCacheKeyPrefix = "search_products_";
        private const string CategoryProductsCacheKeyPrefix = "category_products_";

        public ProductService(
            IProductRepository productRepository, 
            ICategoryRepository categoryRepository, 
            IInventoryRepository inventoryRepository,
            ICacheService cacheService,
            ILogger<ProductService> logger)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _inventoryRepository = inventoryRepository;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<ProductDto?> GetByIdAsync(Guid id)
        {
            var cacheKey = $"{ProductCacheKeyPrefix}{id}";
            var cached = await _cacheService.GetAsync<ProductDto>(cacheKey).ConfigureAwait(false);
            if (cached != null)
            {
                _logger.LogDebug("Product {ProductId} retrieved from cache", id);
                return cached;
            }

            var entity = await _productRepository.GetByIdAsync(id).ConfigureAwait(false);
            if (entity == null) return null;

            var dto = Map(entity);
            await _cacheService.SetAsync(cacheKey, dto, TimeSpan.FromHours(1)).ConfigureAwait(false);
            return dto;
        }

        public async Task<IEnumerable<ProductDto>> GetAllAsync()
        {
            var cached = await _cacheService.GetAsync<IEnumerable<ProductDto>>(AllProductsCacheKey).ConfigureAwait(false);
            if (cached != null)
            {
                _logger.LogDebug("All products retrieved from cache");
                return cached;
            }

            var items = await _productRepository.GetAllAsync().ConfigureAwait(false);
            var dtos = items.Select(Map).ToList();
            await _cacheService.SetAsync(AllProductsCacheKey, (IEnumerable<ProductDto>)dtos, TimeSpan.FromHours(1)).ConfigureAwait(false);
            return dtos;
        }

        public async Task<(IEnumerable<ProductDto> Items, int TotalCount)> SearchAsync(string? searchTerm, Guid? categoryId, int pageNumber, int pageSize, string? sortBy, string? sortDirection)
        {
            // Validate sort parameters
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                var sb = sortBy.Trim().ToLowerInvariant();
                if (sb != "name" && sb != "price" && sb != "createdat" && sb != "created_at")
                {
                    throw new DomainException("Invalid sortBy value. Allowed: name, price, createdAt.");
                }
            }

            if (!string.IsNullOrWhiteSpace(sortDirection))
            {
                var sd = sortDirection.Trim().ToLowerInvariant();
                if (sd != "asc" && sd != "desc")
                {
                    throw new DomainException("Invalid sortDirection value. Allowed: asc, desc.");
                }
            }

            // Create cache key based on search parameters
            var cacheKey = $"{SearchCacheKeyPrefix}{searchTerm}_{categoryId}_{pageNumber}_{pageSize}_{sortBy}_{sortDirection}";

            // Note: We don't cache tuples as they are value types
            // For search results, we perform the query directly
            var (items, total) = await _productRepository.SearchAsync(searchTerm, categoryId, pageNumber, pageSize, sortBy, sortDirection).ConfigureAwait(false);
            return (items.Select(Map), total);
        }


        public async Task<IEnumerable<ProductDto>> GetByCategoryAsync(Guid categoryId)
        {
            var cacheKey = $"{CategoryProductsCacheKeyPrefix}{categoryId}";
            var cached = await _cacheService.GetAsync<IEnumerable<ProductDto>>(cacheKey).ConfigureAwait(false);
            if (cached != null)
            {
                _logger.LogDebug("Category products retrieved from cache for category {CategoryId}", categoryId);
                return cached;
            }

            var items = await _productRepository.GetByCategoryAsync(categoryId).ConfigureAwait(false);
            var dtos = items.Select(Map).ToList();
            await _cacheService.SetAsync(cacheKey, (IEnumerable<ProductDto>)dtos, TimeSpan.FromHours(1)).ConfigureAwait(false);
            return dtos;
        }

        public async Task<ProductDto> CreateAsync(CreateProductDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) throw new DomainException("Product name is required.");
            if (dto.Price < 0) throw new DomainException("Price must be non-negative.");
            if (string.IsNullOrWhiteSpace(dto.SKU)) throw new DomainException("SKU is required.");

            var existing = await _productRepository.GetBySkuAsync(dto.SKU).ConfigureAwait(false);
            if (existing != null) throw new DomainException("A product with the same SKU already exists.");

            var category = await _categoryRepository.GetByIdAsync(dto.CategoryId).ConfigureAwait(false);
            if (category == null) throw new DomainException("Category not found.");

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = dto.Name.Trim(),
                Description = dto.Description ?? string.Empty,
                Price = dto.Price,
                SKU = dto.SKU.Trim(),
                Status = dto.Status,
                CategoryId = dto.CategoryId,
                CreatedAt = DateTime.UtcNow
            };
            await _productRepository.AddAsync(product).ConfigureAwait(false);

            // Initialize inventory record for the new product if none exists
            var existingInventory = await _inventoryRepository.GetByProductIdAsync(product.Id).ConfigureAwait(false);
            if (existingInventory == null)
            {
                var inventory = new Inventory
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Quantity = 0,
                    ReorderLevel = 0,
                    CreatedAt = DateTime.UtcNow
                };
                await _inventoryRepository.AddAsync(inventory).ConfigureAwait(false);
            }

            // Persist both Product and Inventory in a single SaveChanges call
            await _productRepository.SaveChangesAsync().ConfigureAwait(false);

            // Invalidate related cache entries
            await InvalidatProductCacheAsync(product.Id).ConfigureAwait(false);

            return Map(product);
        }

        public async Task UpdateAsync(Guid id, UpdateProductDto dto)
        {
            var product = await _productRepository.GetByIdAsync(id).ConfigureAwait(false);
            if (product == null) throw new DomainException("Product not found.");

            if (!string.IsNullOrWhiteSpace(dto.Name)) product.Name = dto.Name.Trim();
            if (dto.Description != null) product.Description = dto.Description;
            if (dto.Price.HasValue)
            {
                if (dto.Price.Value < 0) throw new DomainException("Price must be non-negative.");
                product.Price = dto.Price.Value;
            }
            if (!string.IsNullOrWhiteSpace(dto.SKU) && dto.SKU.Trim() != product.SKU)
            {
                var existing = await _productRepository.GetBySkuAsync(dto.SKU.Trim()).ConfigureAwait(false);
                if (existing != null && existing.Id != id) throw new DomainException("SKU already in use by another product.");
                product.SKU = dto.SKU.Trim();
            }

            if (dto.CategoryId.HasValue && dto.CategoryId.Value != product.CategoryId)
            {
                var category = await _categoryRepository.GetByIdAsync(dto.CategoryId.Value).ConfigureAwait(false);
                if (category == null) throw new DomainException("Category not found.");
                product.CategoryId = dto.CategoryId.Value;
            }

            product.Status = dto.Status ?? product.Status;
            product.UpdatedAt = DateTime.UtcNow;

            _productRepository.Update(product);
            await _productRepository.SaveChangesAsync().ConfigureAwait(false);

            // Invalidate related cache entries
            await InvalidatProductCacheAsync(id).ConfigureAwait(false);
        }

        public async Task DeleteAsync(Guid id)
        {
            var product = await _productRepository.GetByIdAsync(id).ConfigureAwait(false);
            if (product == null) throw new DomainException("Product not found.");

            _productRepository.Delete(product);
            await _productRepository.SaveChangesAsync().ConfigureAwait(false);

            // Invalidate related cache entries
            await InvalidatProductCacheAsync(id).ConfigureAwait(false);
        }

        private async Task InvalidatProductCacheAsync(Guid productId)
        {
            await _cacheService.RemoveAsync($"{ProductCacheKeyPrefix}{productId}").ConfigureAwait(false);
            await _cacheService.RemoveAsync(AllProductsCacheKey).ConfigureAwait(false);
            await _cacheService.RemoveByPatternAsync($"{CategoryProductsCacheKeyPrefix}.*").ConfigureAwait(false);
            await _cacheService.RemoveByPatternAsync($"{SearchCacheKeyPrefix}.*").ConfigureAwait(false);
            _logger.LogInformation("Cache invalidated for product {ProductId}", productId);
        }

        private static ProductDto Map(Product p)
        {
            return new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                SKU = p.SKU,
                Status = p.Status,
                CategoryId = p.CategoryId,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            };
        }
    }
}
