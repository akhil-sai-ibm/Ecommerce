using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProductService.Application.Interfaces.Repositories;
using ProductService.Domain.Entities;
using ProductService.Infrastructure.Data;

namespace ProductService.Infrastructure.Repositories
{
    public class InventoryReservationRepository : IInventoryReservationRepository
    {
        private readonly ProductServiceDbContext _context;

        public InventoryReservationRepository(ProductServiceDbContext context)
        {
            _context = context;
        }

        public async Task<InventoryReservation?> GetByReservationIdAsync(Guid reservationId)
        {
            return await _context.InventoryReservations
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.ReservationId == reservationId);
        }

        public async Task AddAsync(InventoryReservation reservation)
        {
            await _context.InventoryReservations.AddAsync(reservation);
        }

        public void Update(InventoryReservation reservation)
        {
            _context.InventoryReservations.Update(reservation);
        }
    }
}
