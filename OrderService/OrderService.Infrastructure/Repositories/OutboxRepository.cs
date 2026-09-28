using Microsoft.EntityFrameworkCore;
using OrderService.Application.Interfaces.Repositories;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderService.Infrastructure.Repositories
{
    public class OutboxRepository : IOutboxRepository
    {
        private readonly OrderServiceDbContext _context;

        public OutboxRepository(OrderServiceDbContext context)
        {
            _context = context;
        }

        public async Task<OutboxMessage> AddAsync(OutboxMessage message)
        {
            await _context.OutboxMessages.AddAsync(message);
            return message;
        }

        public async Task<IEnumerable<OutboxMessage>> GetUnprocessedMessagesAsync(int count)
        {
            return await _context.OutboxMessages
                .Where(x => !x.Processed)
                .OrderBy(x => x.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public Task UpdateAsync(OutboxMessage message)
        {
            _context.OutboxMessages.Update(message);
            return Task.CompletedTask;
        }
    }
}
