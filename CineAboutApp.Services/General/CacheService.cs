using CineAboutApp.Interfaces.General;
using Microsoft.Extensions.Caching.Memory;

namespace CineAboutApp.Services.General
{
    public class CacheService(IMemoryCache memoryCache) : ICacheService
    {
        private readonly IMemoryCache _memoryCache = memoryCache;
        private static readonly HashSet<string> _allKeys = new();

        public async Task<T?> GetAsync<T>(string key)
        {
            return await Task.FromResult(_memoryCache.TryGetValue(key, out T? value) ? value : default);
        }

        public async Task SetAsync<T>(string key, T value, int? ttlMinutes = null)
        {
            var cacheOptions = new MemoryCacheEntryOptions();
            
            if (ttlMinutes.HasValue)
            {
                cacheOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(ttlMinutes.Value);
            }
            else
            {
                cacheOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
            }

            _memoryCache.Set(key, value, cacheOptions);
            _allKeys.Add(key);
            
            await Task.CompletedTask;
        }

        public async Task RemoveAsync(string key)
        {
            _memoryCache.Remove(key);
            _allKeys.Remove(key);
            await Task.CompletedTask;
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            var keysToRemove = new List<string>();
            
            foreach (var key in _allKeys)
            {
                if (key.StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
                _allKeys.Remove(key);
            }
            
            await Task.CompletedTask;
        }
    }
}

