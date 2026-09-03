using System;
using System.Threading.Tasks;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;

namespace ECommerce.Infrastructure.Repositories
{
    /// <summary>
    /// Repository for idempotency records.
    /// Manages concurrent idempotency requests safely at the database level.
    /// </summary>
    public class IdempotencyRepository : GenericRepository<IdempotencyRecord>, IIdempotencyRepository
    {
        public IdempotencyRepository(ECommerceDbContext context) : base(context)
        {
        }

        public async Task<IdempotencyRecord?> GetByKeyAndUserIdAsync(string key, Guid userId)
        {
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(ir => ir.Key == key && ir.UserId == userId)
                .ConfigureAwait(false);
        }

        public async Task<IdempotencyRecord> GetOrCreateAsync(string key, Guid userId, string endpoint, string method)
        {
            // First, try to get existing record
            var existing = await GetByKeyAndUserIdAsync(key, userId).ConfigureAwait(false);
            if (existing != null)
            {
                return existing;
            }

            // Create new record
            // The unique constraint (UserId, Key) will prevent duplicates if concurrent requests
            // try to create the same idempotency record simultaneously
            var record = new IdempotencyRecord
            {
                Id = Guid.NewGuid(),
                Key = key,
                UserId = userId,
                Endpoint = endpoint,
                Method = method,
                CreatedAt = DateTime.UtcNow,
                Succeeded = false,
                OrderId = null,
                ErrorMessage = null,
                ResponseStatusCode = 0
            };

            await AddAsync(record).ConfigureAwait(false);

            // Note: Intentionally NOT calling SaveChangesAsync here.
            // SaveChangesAsync is called by OrderService within the transaction.
            // If duplicate constraint violation occurs, it will be caught during SaveChangesAsync.

            return record;
        }
    }
}
