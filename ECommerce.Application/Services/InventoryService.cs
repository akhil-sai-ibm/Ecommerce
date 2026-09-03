using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Application.DTOs.Inventory;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;

namespace ECommerce.Application.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IInventoryRepository _inventoryRepository;
        private readonly IProductRepository _productRepository;

        public InventoryService(IInventoryRepository inventoryRepository, IProductRepository productRepository)
        {
            _inventoryRepository = inventoryRepository;
            _productRepository = productRepository;
        }

        public async Task<IEnumerable<InventoryDto>> GetLowStockItemsAsync()
        {
            var items = await _inventoryRepository.GetLowStockItemsAsync().ConfigureAwait(false);
            return items.Select(Map);
        }

        public async Task<InventoryDto?> GetByIdAsync(Guid id)
        {
            var inv = await _inventoryRepository.GetByIdAsync(id).ConfigureAwait(false);
            if (inv == null) return null;
            return Map(inv);
        }

        public async Task<InventoryDto?> GetByProductIdAsync(Guid productId)
        {
            var inv = await _inventoryRepository.GetByProductIdAsync(productId).ConfigureAwait(false);
            if (inv == null) return null;
            return Map(inv);
        }

        public async Task<IEnumerable<InventoryDto>> GetAllAsync()
        {
            var items = await _inventoryRepository.GetAllAsync().ConfigureAwait(false);
            return items.Select(Map);
        }

        public async Task<InventoryDto> AddAsync(CreateInventoryDto dto)
        {
            if (dto.Quantity < 0) throw new DomainException("Quantity cannot be negative.");
            if (dto.ReorderLevel < 0) throw new DomainException("Reorder level cannot be negative.");

            var product = await _productRepository.GetByIdAsync(dto.ProductId).ConfigureAwait(false);
            if (product == null) throw new DomainException("Product not found.");

            var entity = new Inventory
            {
                Id = Guid.NewGuid(),
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                ReorderLevel = dto.ReorderLevel,
                CreatedAt = DateTime.UtcNow
            };

            await _inventoryRepository.AddAsync(entity).ConfigureAwait(false);
            await _inventoryRepository.SaveChangesAsync().ConfigureAwait(false);

            return Map(entity);
        }

        public async Task UpdateAsync(Guid id, UpdateInventoryDto dto)
        {
            var entity = await _inventoryRepository.GetByIdAsync(id).ConfigureAwait(false);
            if (entity == null) throw new DomainException("Inventory not found.");

            if (dto.Quantity.HasValue)
            {
                if (dto.Quantity.Value < 0) throw new DomainException("Quantity cannot be negative.");
                entity.Quantity = dto.Quantity.Value;
            }

            if (dto.ReorderLevel.HasValue)
            {
                if (dto.ReorderLevel.Value < 0) throw new DomainException("Reorder level cannot be negative.");
                entity.ReorderLevel = dto.ReorderLevel.Value;
            }

            entity.UpdatedAt = DateTime.UtcNow;
            _inventoryRepository.Update(entity);
            await _inventoryRepository.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task IncreaseStockAsync(Guid productId, int amount)
        {
            if (amount <= 0) throw new DomainException("Amount must be positive.");

            var inv = await _inventoryRepository.GetByProductIdAsync(productId).ConfigureAwait(false);
            if (inv == null) throw new DomainException("Inventory record not found.");

            inv.Quantity += amount;
            _inventoryRepository.Update(inv);
            await _inventoryRepository.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task DecreaseStockAsync(Guid productId, int amount)
        {
            if (amount <= 0) throw new DomainException("Amount must be positive.");

            var inv = await _inventoryRepository.GetByProductIdAsync(productId).ConfigureAwait(false);
            if (inv == null) throw new DomainException("Inventory record not found.");

            if (inv.Quantity - amount < 0) throw new DomainException("Insufficient stock.");

            inv.Quantity -= amount;
            _inventoryRepository.Update(inv);
            await _inventoryRepository.SaveChangesAsync().ConfigureAwait(false);
        }

        private static InventoryDto Map(Inventory i) => new InventoryDto { Id = i.Id, ProductId = i.ProductId, Quantity = i.Quantity, ReorderLevel = i.ReorderLevel };
    }
}
