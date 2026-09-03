using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Application.DTOs.Inventory;

namespace ECommerce.Application.Interfaces.Services
{
    public interface IInventoryService
    {
        Task<InventoryDto?> GetByIdAsync(Guid id);
        Task<InventoryDto?> GetByProductIdAsync(Guid productId);
        Task<IEnumerable<InventoryDto>> GetAllAsync();
        Task<InventoryDto> AddAsync(CreateInventoryDto dto);
        Task UpdateAsync(Guid id, UpdateInventoryDto dto);
        Task IncreaseStockAsync(Guid productId, int amount);
        Task DecreaseStockAsync(Guid productId, int amount);
        Task<IEnumerable<InventoryDto>> GetLowStockItemsAsync();
    }
}
