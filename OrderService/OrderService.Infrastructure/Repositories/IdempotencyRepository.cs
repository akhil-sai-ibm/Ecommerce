using Microsoft.EntityFrameworkCore;
using OrderService.Application.Interfaces.Repositories;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Data;
using System;
using System.Threading.Tasks;

namespace OrderService.Infrastructure.Repositories
{
    public class IdempotencyRepository : IIdempotencyRepository
    {
        private readonly OrderServiceDbContext _context;

        public IdempotencyRepository(OrderServiceDbContext context)
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
