using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProductService.Application.DTOs.Products;
using ProductService.Domain.Enums;

namespace ProductService.Application.Interfaces.Services
{
    public interface IProductService
    {
        Task<ProductDto?> GetByIdAsync(Guid id);

        Task<IEnumerable<ProductDto>> GetAllAsync();

        Task<(IEnumerable<ProductDto> Items, int TotalCount)> SearchAsync(string? searchTerm, Guid? categoryId, int pageNumber, int pageSize, string? sortBy, string? sortDirection, ProductStatus? status = null);

        Task<IEnumerable<ProductDto>> GetByCategoryAsync(Guid categoryId);

        Task<BulkProductUploadResultDto> BulkUploadAsync(System.IO.Stream excelStream);
        Task<IEnumerable<ProductDto>> GetBulkProductsAsync(IEnumerable<Guid> productIds);

        Task<ProductDto> CreateAsync(CreateProductDto dto);

        Task UpdateAsync(Guid id, UpdateProductDto dto);

        Task<ProductDeleteResultDto> DeleteAsync(Guid id);

        Task<string> UploadProductImageAsync(Guid id, global::ProductService.Application.DTOs.Common.FileData file);
        Task DeleteProductImageAsync(Guid id);
    }
}

