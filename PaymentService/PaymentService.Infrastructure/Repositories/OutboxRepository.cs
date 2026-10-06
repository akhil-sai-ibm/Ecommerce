using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Domain.Entities;
using PaymentService.Infrastructure.Data;

namespace PaymentService.Infrastructure.Repositories
{
    public class OutboxRepository : IOutboxRepository
    {
        private readonly PaymentServiceDbContext _context;

        public OutboxRepository(PaymentServiceDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(OutboxMessage message)
        {
            await _context.OutboxMessages.AddAsync(message);
        }

        public async Task<IEnumerable<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize)
        {
            return await _context.OutboxMessages
                .Where(m => !m.Processed)
                .OrderBy(m => m.CreatedAt)
                .Take(batchSize)
                .ToListAsync();
        }

        public Task UpdateAsync(OutboxMessage message)
        {
            _context.OutboxMessages.Update(message);
            return Task.CompletedTask;
        }
    }
}
