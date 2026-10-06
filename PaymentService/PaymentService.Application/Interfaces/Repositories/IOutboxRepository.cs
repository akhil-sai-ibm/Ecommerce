using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PaymentService.Domain.Entities;

namespace PaymentService.Application.Interfaces.Repositories
{
    public interface IOutboxRepository
    {
        Task AddAsync(OutboxMessage message);
        Task<IEnumerable<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize);
        Task UpdateAsync(OutboxMessage message);
    }
}
