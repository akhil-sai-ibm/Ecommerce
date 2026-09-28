using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProductService.Application.DTOs.Inventory;
using ProductService.Application.Interfaces.Repositories;
using ProductService.Application.Interfaces.Services;
using ProductService.Domain.Entities;
using ProductService.Domain.Exceptions;

namespace ProductService.Application.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IInventoryRepository _inventoryRepository;
        private readonly IProductRepository _productRepository;
        private readonly IInventoryReservationRepository _reservationRepository;

        public InventoryService(IInventoryRepository inventoryRepository, IProductRepository productRepository, IInventoryReservationRepository reservationRepository)
        {
            _inventoryRepository = inventoryRepository;
            _productRepository = productRepository;
            _reservationRepository = reservationRepository;
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

        public async Task<ReservationResultDto> ReserveInventoryAsync(ReservationRequestDto dto)
        {
            var result = new ReservationResultDto { Success = true };
            
            // Check idempotency
            var existing = await _reservationRepository.GetByReservationIdAsync(dto.ReservationId).ConfigureAwait(false);
            if (existing != null)
            {
                if (existing.Status == Domain.Enums.ReservationStatus.Pending || existing.Status == Domain.Enums.ReservationStatus.Confirmed)
                {
                    // Already reserved successfully
                    return result; 
                }
                if (existing.Status == Domain.Enums.ReservationStatus.Released)
                {
                    result.Success = false;
                    result.Message = "Reservation was already released.";
                    return result;
                }
            }

            // Verify stock
            var inventoryUpdates = new List<Inventory>();
            foreach (var item in dto.Items)
            {
                var inv = await _inventoryRepository.GetByProductIdAsync(item.ProductId).ConfigureAwait(false);
                if (inv == null || inv.Quantity < item.Quantity)
                {
                    result.Success = false;
                    result.MissingItems.Add(item.ProductId.ToString());
                }
                else if (result.Success)
                {
                    inv.Quantity -= item.Quantity;
                    inventoryUpdates.Add(inv);
                }
            }

            if (!result.Success)
            {
                result.Message = "Insufficient inventory for one or more items.";
                return result;
            }

            // Deduct stock and save reservation
            var reservation = new InventoryReservation
            {
                Id = Guid.NewGuid(),
                ReservationId = dto.ReservationId,
                Status = Domain.Enums.ReservationStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                Items = dto.Items.Select(x => new InventoryReservationItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = x.ProductId,
                    Quantity = x.Quantity
                }).ToList()
            };

            await _reservationRepository.AddAsync(reservation).ConfigureAwait(false);
            
            foreach (var inv in inventoryUpdates)
            {
                _inventoryRepository.Update(inv);
            }

            await _inventoryRepository.SaveChangesAsync().ConfigureAwait(false);
            return result;
        }

        public async Task ReleaseInventoryAsync(Guid reservationId)
        {
            var reservation = await _reservationRepository.GetByReservationIdAsync(reservationId).ConfigureAwait(false);
            if (reservation == null) return; // Nothing to release
            if (reservation.Status != Domain.Enums.ReservationStatus.Pending) return; // Only pending can be released

            foreach (var item in reservation.Items)
            {
                var inv = await _inventoryRepository.GetByProductIdAsync(item.ProductId).ConfigureAwait(false);
                if (inv != null)
                {
                    inv.Quantity += item.Quantity;
                    _inventoryRepository.Update(inv);
                }
            }

            reservation.Status = Domain.Enums.ReservationStatus.Released;
            _reservationRepository.Update(reservation);

            await _inventoryRepository.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task ConfirmInventoryAsync(Guid reservationId)
        {
            var reservation = await _reservationRepository.GetByReservationIdAsync(reservationId).ConfigureAwait(false);
            if (reservation == null) return;
            if (reservation.Status != Domain.Enums.ReservationStatus.Pending) return;

            reservation.Status = Domain.Enums.ReservationStatus.Confirmed;
            _reservationRepository.Update(reservation);

            await _inventoryRepository.SaveChangesAsync().ConfigureAwait(false);
        }

        private static InventoryDto Map(Inventory i) => new InventoryDto { Id = i.Id, ProductId = i.ProductId, Quantity = i.Quantity, ReorderLevel = i.ReorderLevel };
    }
}

