using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProductService.Application.DTOs.Inventory;

namespace ProductService.Application.Interfaces.Services
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
        Task<ReservationResultDto> ReserveInventoryAsync(ReservationRequestDto dto);
        Task ReleaseInventoryAsync(Guid reservationId);
        Task ConfirmInventoryAsync(Guid reservationId);
    }
}

