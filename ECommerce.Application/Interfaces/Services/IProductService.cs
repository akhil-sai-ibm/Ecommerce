using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Application.DTOs.Products;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Interfaces.Services
{
    public interface IProductService
    {
        Task<ProductDto?> GetByIdAsync(Guid id);

        Task<IEnumerable<ProductDto>> GetAllAsync();

        Task<(IEnumerable<ProductDto> Items, int TotalCount)> SearchAsync(string? searchTerm, Guid? categoryId, int pageNumber, int pageSize, string? sortBy, string? sortDirection, ProductStatus? status = null);

        Task<IEnumerable<ProductDto>> GetByCategoryAsync(Guid categoryId);

        Task<BulkProductUploadResultDto> BulkUploadAsync(System.IO.Stream excelStream);

        Task<ProductDto> CreateAsync(CreateProductDto dto);

        Task UpdateAsync(Guid id, UpdateProductDto dto);

        Task<ProductDeleteResultDto> DeleteAsync(Guid id);

        Task<string> UploadProductImageAsync(Guid id, ECommerce.Application.DTOs.Common.FileData file);
        Task DeleteProductImageAsync(Guid id);
    }
}
