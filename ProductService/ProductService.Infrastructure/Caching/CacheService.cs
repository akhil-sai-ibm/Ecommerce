using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ProductService.Application.Interfaces.Services;

namespace ProductService.Infrastructure.Caching
{
    /// <summary>
    /// In-memory cache service implementation using IMemoryCache.
    /// Provides cache operations for application data caching.
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CacheService> _logger;
        private readonly HashSet<string> _cacheKeys = new();

        public CacheService(IMemoryCache memoryCache, ILogger<CacheService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            return await Task.Run(() =>
            {
                if (_memoryCache.TryGetValue(key, out T? value))
                {
                    _logger.LogDebug("Cache hit for key: {Key}", key);
                    return value;
                }

                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }).ConfigureAwait(false);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            await Task.Run(() =>
            {
                var cacheOptions = new MemoryCacheEntryOptions();

                if (expiration.HasValue)
                {
                    cacheOptions.AbsoluteExpirationRelativeToNow = expiration;
                }
                else
                {
                    // Default 30 minute expiration
                    cacheOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                }

                _memoryCache.Set(key, value, cacheOptions);
                _cacheKeys.Add(key);

                _logger.LogDebug("Cache set for key: {Key} with expiration: {Expiration}", key, expiration?.TotalSeconds ?? 1800);
            }).ConfigureAwait(false);
        }

        public async Task RemoveAsync(string key)
        {
            await Task.Run(() =>
            {
                _memoryCache.Remove(key);
                _cacheKeys.Remove(key);
                _logger.LogDebug("Cache removed for key: {Key}", key);
            }).ConfigureAwait(false);
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            await Task.Run(() =>
            {
                try
                {
                    var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                    var keysToRemove = _cacheKeys.Where(k => regex.IsMatch(k)).ToList();

                    foreach (var key in keysToRemove)
                    {
                        _memoryCache.Remove(key);
                        _cacheKeys.Remove(key);
                    }

                    _logger.LogDebug("Cache removed {Count} keys matching pattern: {Pattern}", keysToRemove.Count, pattern);
                }
                catch (RegexParseException ex)
                {
                    _logger.LogWarning(ex, "Invalid regex pattern for cache removal: {Pattern}", pattern);
                }
            }).ConfigureAwait(false);
        }

        public async Task<bool> ExistsAsync(string key)
        {
            return await Task.Run(() =>
            {
                return _memoryCache.TryGetValue(key, out _);
            }).ConfigureAwait(false);
        }
    }
}

