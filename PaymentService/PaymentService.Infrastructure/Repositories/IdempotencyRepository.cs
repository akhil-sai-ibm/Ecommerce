using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Domain.Entities;
using PaymentService.Infrastructure.Data;
using System;
using System.Threading.Tasks;

namespace PaymentService.Infrastructure.Repositories
{
    public class IdempotencyRepository : IIdempotencyRepository
    {
        private readonly PaymentServiceDbContext _context;

        public IdempotencyRepository(PaymentServiceDbContext context)
        {
            _context = context;
        }

        public async Task<IdempotencyRecord> AddAsync(IdempotencyRecord record)
        {
            await _context.IdempotencyRecords.AddAsync(record);
            return record;
        }

        public async Task<IdempotencyRecord?> GetAsync(Guid userId, string key, string requestPath)
        {
            return await _context.IdempotencyRecords
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Key == key && x.RequestPath == requestPath);
        }

        public Task UpdateAsync(IdempotencyRecord record)
        {
            _context.IdempotencyRecords.Update(record);
            return Task.CompletedTask;
        }
    }
}
