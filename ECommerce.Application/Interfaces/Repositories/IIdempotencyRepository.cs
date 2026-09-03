using System;
using System.Threading.Tasks;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Interfaces.Repositories
{
    /// <summary>
    /// Repository for managing idempotency records to prevent duplicate checkout requests.
    /// </summary>
    public interface IIdempotencyRepository : IGenericRepository<IdempotencyRecord>
    {
        /// <summary>
        /// Attempts to find an existing idempotency record for the given key and user.
        /// </summary>
        /// <param name="key">The idempotency key (provided by the client)</param>
        /// <param name="userId">The user ID who initiated the request</param>
        /// <returns>Existing idempotency record if found; null otherwise</returns>
        Task<IdempotencyRecord?> GetByKeyAndUserIdAsync(string key, Guid userId);

        /// <summary>
        /// Creates or retrieves an idempotency record using optimistic concurrency.
        /// If the record already exists for this key+userId, returns the existing one.
        /// </summary>
        /// <param name="key">The idempotency key</param>
        /// <param name="userId">The user ID</param>
        /// <param name="endpoint">The HTTP endpoint (for audit purposes)</param>
        /// <param name="method">The HTTP method (for audit purposes)</param>
        /// <returns>New or existing idempotency record</returns>
        Task<IdempotencyRecord> GetOrCreateAsync(string key, Guid userId, string endpoint, string method);
    }
}
