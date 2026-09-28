using System;
using System.Threading.Tasks;
using ProductService.Domain.Entities;

namespace ProductService.Application.Interfaces.Repositories
{
    public interface IInventoryReservationRepository
    {
        Task<InventoryReservation?> GetByReservationIdAsync(Guid reservationId);
        Task AddAsync(InventoryReservation reservation);
        void Update(InventoryReservation reservation);
    }
}
