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
        private readonly IOrderRepository _orderRepository;
        private readonly ICartRepository _cartRepository;
        private readonly ICacheService _cacheService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;
        private readonly ECommerce.Application.Configuration.BulkProductOptions _bulkOptions;
        private readonly ILogger<ProductService> _logger;

        private const string ProductCacheKeyPrefix = "product_";
        private const string AllProductsCacheKey = "all_products";
        private const string SearchCacheKeyPrefix = "search_products_";
        private const string CategoryProductsCacheKeyPrefix = "category_products_";

        public ProductService(
            IProductRepository productRepository, 
            IOrderRepository orderRepository,
            ICartRepository cartRepository,
            ICategoryRepository categoryRepository, 
            IInventoryRepository inventoryRepository,
            ICacheService cacheService,
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService,
            Microsoft.Extensions.Options.IOptions<ECommerce.Application.Configuration.BulkProductOptions> bulkOptions,
            ILogger<ProductService> logger)
        {
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _cartRepository = cartRepository;
            _categoryRepository = categoryRepository;
            _inventoryRepository = inventoryRepository;
            _cacheService = cacheService;
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
            _bulkOptions = bulkOptions.Value;
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

        public async Task<(IEnumerable<ProductDto> Items, int TotalCount)> SearchAsync(string? searchTerm, Guid? categoryId, int pageNumber, int pageSize, string? sortBy, string? sortDirection, ProductStatus? status = null)
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
            var cacheKey = $"{SearchCacheKeyPrefix}{searchTerm}_{categoryId}_{pageNumber}_{pageSize}_{sortBy}_{sortDirection}_{status}";

            // Note: We don't cache tuples as they are value types
            // For search results, we perform the query directly
            var (items, total) = await _productRepository.SearchAsync(searchTerm, categoryId, pageNumber, pageSize, sortBy, sortDirection, status).ConfigureAwait(false);
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

        public async Task<BulkProductUploadResultDto> BulkUploadAsync(System.IO.Stream excelStream)
        {
            var result = new BulkProductUploadResultDto();

            // Load all categories for resolution
            var categories = await _categoryRepository.GetAllAsync().ConfigureAwait(false);
            var categoryDict = categories.ToDictionary(c => c.Name.Trim(), c => c.Id, StringComparer.OrdinalIgnoreCase);

            var productsToInsert = new List<Product>();
            var inventoriesToInsert = new List<Inventory>();
            var skusInUpload = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Register encoding provider (required for ExcelDataReader on some platforms/files)
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using (var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(excelStream))
            {
                var dataSet = ExcelDataReader.ExcelDataReaderExtensions.AsDataSet(reader, new ExcelDataReader.ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataReader.ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = true
                    }
                });

                if (dataSet.Tables.Count == 0)
                {
                    throw new DomainException("Uploaded Excel file is empty.");
                }

                var table = dataSet.Tables[0];
                int rowNumber = 1; // Header is row 1 conceptually in Excel, data starts at 2

                foreach (System.Data.DataRow row in table.Rows)
                {
                    rowNumber++;
                    result.TotalRows++;

                    try
                    {
                        var name = row.Table.Columns.Contains("Name") ? row["Name"]?.ToString()?.Trim() : null;
                        var description = row.Table.Columns.Contains("Description") ? row["Description"]?.ToString()?.Trim() : string.Empty;
                        var priceStr = row.Table.Columns.Contains("Price") ? row["Price"]?.ToString() : null;
                        var sku = row.Table.Columns.Contains("SKU") ? row["SKU"]?.ToString()?.Trim() : null;
                        var categoryName = row.Table.Columns.Contains("Category") ? row["Category"]?.ToString()?.Trim() : null;
                        var statusStr = row.Table.Columns.Contains("Status") ? row["Status"]?.ToString()?.Trim() : "Active";
                        var quantityStr = row.Table.Columns.Contains("Quantity") ? row["Quantity"]?.ToString() : "0";
                        var reorderLevelStr = row.Table.Columns.Contains("ReorderLevel") ? row["ReorderLevel"]?.ToString() : "0";

                        // Validation
                        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Product name is required.");
                        if (string.IsNullOrWhiteSpace(sku)) throw new DomainException("SKU is required.");
                        if (string.IsNullOrWhiteSpace(categoryName)) throw new DomainException("Category is required.");
                        
                        if (!decimal.TryParse(priceStr, out decimal price) || price <= 0)
                            throw new DomainException("Price must be greater than zero.");

                        if (!int.TryParse(quantityStr, out int quantity) || quantity < 0)
                            throw new DomainException("Quantity must not be negative.");

                        if (!int.TryParse(reorderLevelStr, out int reorderLevel) || reorderLevel < 0)
                            throw new DomainException("ReorderLevel must not be negative.");

                        if (string.IsNullOrWhiteSpace(statusStr)) statusStr = "Active";
                        if (!Enum.TryParse<ProductStatus>(statusStr, true, out var status))
                            throw new DomainException($"Invalid product status: {statusStr}.");

                        if (!categoryDict.TryGetValue(categoryName, out Guid categoryId))
                            throw new DomainException($"Category '{categoryName}' not found.");

                        if (!skusInUpload.Add(sku))
                            throw new DomainException("Duplicate SKU within the uploaded file.");

                        var productId = Guid.NewGuid();
                        productsToInsert.Add(new Product
                        {
                            Id = productId,
                            Name = name,
                            Description = description ?? string.Empty,
                            Price = price,
                            SKU = sku,
                            CategoryId = categoryId,
                            Status = status,
                            CreatedAt = DateTime.UtcNow
                        });

                        inventoriesToInsert.Add(new Inventory
                        {
                            Id = Guid.NewGuid(),
                            ProductId = productId,
                            Quantity = quantity,
                            ReorderLevel = reorderLevel,
                            CreatedAt = DateTime.UtcNow
                        });

                        result.SuccessfulRows++;
                    }
                    catch (Exception ex)
                    {
                        result.FailedRows++;
                        result.Errors.Add(new BulkProductErrorDto
                        {
                            RowNumber = rowNumber,
                            Sku = row.Table.Columns.Contains("SKU") ? row["SKU"]?.ToString() : null,
                            Error = ex is DomainException ? ex.Message : "Invalid data format."
                        });
                    }
                }
            }

            // Batch Processing
            if (productsToInsert.Any())
            {
                var batchSize = _bulkOptions.BatchSize > 0 ? _bulkOptions.BatchSize : 100;
                
                var existingProducts = await _productRepository.GetAllAsync().ConfigureAwait(false);
                var existingSkus = new HashSet<string>(existingProducts.Select(p => p.SKU), StringComparer.OrdinalIgnoreCase);

                var finalProductsToInsert = new List<Product>();
                var finalInventoriesToInsert = new List<Inventory>();

                for (int i = 0; i < productsToInsert.Count; i++)
                {
                    var product = productsToInsert[i];
                    if (existingSkus.Contains(product.SKU))
                    {
                        result.SuccessfulRows--;
                        result.FailedRows++;
                        result.Errors.Add(new BulkProductErrorDto
                        {
                            RowNumber = -1, // We lost exact row, but SKU is what matters
                            Sku = product.SKU,
                            Error = "SKU already exists in the database."
                        });
                    }
                    else
                    {
                        finalProductsToInsert.Add(product);
                        finalInventoriesToInsert.Add(inventoriesToInsert[i]);
                    }
                }

                for (int i = 0; i < finalProductsToInsert.Count; i += batchSize)
                {
                    var productBatch = finalProductsToInsert.Skip(i).Take(batchSize).ToList();
                    var inventoryBatch = finalInventoriesToInsert.Skip(i).Take(batchSize).ToList();

                    await using var transaction = await _unitOfWork.BeginTransactionAsync().ConfigureAwait(false);
                    try
                    {
                        await _productRepository.AddRangeAsync(productBatch).ConfigureAwait(false);
                        await _inventoryRepository.AddRangeAsync(inventoryBatch).ConfigureAwait(false);
                        
                        await _unitOfWork.SaveChangesAsync().ConfigureAwait(false);
                        await transaction.CommitAsync().ConfigureAwait(false);
                        result.BatchesProcessed++;
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync().ConfigureAwait(false);
                        _logger.LogError(ex, "Failed to insert bulk products batch.");
                        throw new DomainException("Database error during bulk insert.");
                    }
                }

                await InvalidatProductCacheAsync(Guid.Empty).ConfigureAwait(false);
            }

            return result;
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
                ImageUrl = dto.ImageUrl,
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
            if (dto.ImageUrl != null) product.ImageUrl = dto.ImageUrl;

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

        public async Task<ProductDeleteResultDto> DeleteAsync(Guid id)
        {
            var product = await _productRepository.GetByIdAsync(id).ConfigureAwait(false);
            if (product == null) throw new DomainException("Product not found.");

            bool hasOrders = await _orderRepository.HasOrderItemsAsync(id).ConfigureAwait(false);

            if (hasOrders)
            {
                // Soft delete
                product.Status = ProductStatus.Inactive;
                product.UpdatedAt = DateTime.UtcNow;
                _productRepository.Update(product);
                await _productRepository.SaveChangesAsync().ConfigureAwait(false);

                await InvalidatProductCacheAsync(id).ConfigureAwait(false);

                return new ProductDeleteResultDto
                {
                    Success = true,
                    Message = "Product deactivated successfully because it has associated orders.",
                    IsSoftDeleted = true
                };
            }
            else
            {
                // Hard delete
                var oldImageUrl = product.ImageUrl;
                
                try 
                {
                    await _cartRepository.RemoveCartItemsByProductIdAsync(id).ConfigureAwait(false);
                    
                    if (product.Inventory != null)
                    {
                        _inventoryRepository.Delete(product.Inventory);
                    }

                    _productRepository.Delete(product);
                    await _unitOfWork.SaveChangesAsync().ConfigureAwait(false);
                } 
                catch (Exception ex) when (ex.GetType().Name == "DbUpdateException")
                {
                    _logger.LogWarning(ex, "Failed to hard delete product {ProductId} due to concurrent FK constraints. Falling back to soft delete.", id);
                    product.Status = ProductStatus.Inactive;
                    product.UpdatedAt = DateTime.UtcNow;
                    _productRepository.Update(product);
                    await _unitOfWork.SaveChangesAsync().ConfigureAwait(false);

                    await InvalidatProductCacheAsync(id).ConfigureAwait(false);

                    return new ProductDeleteResultDto
                    {
                        Success = true,
                        Message = "Product deactivated successfully after a concurrent conflict.",
                        IsSoftDeleted = true
                    };
                }

                if (!string.IsNullOrEmpty(oldImageUrl))
                {
                    try
                    {
                        await _fileStorageService.DeleteFileAsync(oldImageUrl).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete physical image file {ImageUrl} for deleted product {ProductId}.", oldImageUrl, id);
                    }
                }

                await InvalidatProductCacheAsync(id).ConfigureAwait(false);

                return new ProductDeleteResultDto
                {
                    Success = true,
                    Message = "Product deleted successfully.",
                    IsSoftDeleted = false
                };
            }
        }

        public async Task<string> UploadProductImageAsync(Guid id, ECommerce.Application.DTOs.Common.FileData file)
        {
            var product = await _productRepository.GetByIdAsync(id).ConfigureAwait(false);
            if (product == null) throw new DomainException("Product not found.");

            // Validate magic bytes
            if (!ECommerce.Application.Utils.ImageValidator.IsValidImage(file.Content))
            {
                throw new DomainException("Invalid image format. Supported formats are JPG, PNG, WebP.");
            }

            var oldImageUrl = product.ImageUrl;

            var newUrl = await _fileStorageService.SaveFileAsync(file, "products").ConfigureAwait(false);

            product.ImageUrl = newUrl;
            product.UpdatedAt = DateTime.UtcNow;

            _productRepository.Update(product);
            await _productRepository.SaveChangesAsync().ConfigureAwait(false);

            if (!string.IsNullOrEmpty(oldImageUrl))
            {
                await _fileStorageService.DeleteFileAsync(oldImageUrl).ConfigureAwait(false);
            }

            await InvalidatProductCacheAsync(id).ConfigureAwait(false);

            return newUrl;
        }

        public async Task DeleteProductImageAsync(Guid id)
        {
            var product = await _productRepository.GetByIdAsync(id).ConfigureAwait(false);
            if (product == null) throw new DomainException("Product not found.");

            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                await _fileStorageService.DeleteFileAsync(product.ImageUrl).ConfigureAwait(false);
                product.ImageUrl = null;
                product.UpdatedAt = DateTime.UtcNow;

                _productRepository.Update(product);
                await _productRepository.SaveChangesAsync().ConfigureAwait(false);
                
                await InvalidatProductCacheAsync(id).ConfigureAwait(false);
            }
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
                ImageUrl = p.ImageUrl,
                Status = p.Status,
                StockQuantity = p.Inventory?.Quantity ?? 0,
                CategoryId = p.CategoryId,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            };
        }
    }
}
