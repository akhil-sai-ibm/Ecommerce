using PaymentService.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace PaymentService.Application.Interfaces.Repositories
{
    public interface IIdempotencyRepository
    {
        Task<IdempotencyRecord?> GetAsync(Guid userId, string key, string requestPath);
        Task<IdempotencyRecord> AddAsync(IdempotencyRecord record);
        Task UpdateAsync(IdempotencyRecord record);
    }
}
