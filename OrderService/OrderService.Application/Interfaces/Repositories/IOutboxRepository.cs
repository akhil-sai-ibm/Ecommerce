using OrderService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrderService.Application.Interfaces.Repositories
{
    public interface IOutboxRepository
    {
        Task<OutboxMessage> AddAsync(OutboxMessage message);
        Task<IEnumerable<OutboxMessage>> GetUnprocessedMessagesAsync(int count);
        Task UpdateAsync(OutboxMessage message);
    }
}
