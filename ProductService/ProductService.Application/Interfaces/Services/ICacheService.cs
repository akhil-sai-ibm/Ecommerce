using System;
using System.Threading.Tasks;

namespace ProductService.Application.Interfaces.Services
{
    /// <summary>
    /// Cache service abstraction for caching application data.
    /// Implementations should handle in-memory, distributed, or hybrid caching strategies.
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Get a value from cache, or null if not found or expired.
        /// </summary>
        Task<T?> GetAsync<T>(string key) where T : class;

        /// <summary>
        /// Set a value in cache with an absolute expiration.
        /// </summary>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;

        /// <summary>
        /// Remove a specific key from cache.
        /// </summary>
        Task RemoveAsync(string key);

        /// <summary>
        /// Remove all keys matching a pattern from cache.
        /// </summary>
        Task RemoveByPatternAsync(string pattern);

        /// <summary>
        /// Check if a key exists in cache.
        /// </summary>
        Task<bool> ExistsAsync(string key);
    }
}

