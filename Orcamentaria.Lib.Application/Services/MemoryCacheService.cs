using Microsoft.Extensions.Caching.Memory;
using Orcamentaria.Lib.Domain.Services;

namespace Orcamentaria.Lib.Application.Services
{
    public class MemoryCacheService : IMemoryCacheService
    {
        private readonly IMemoryCache _memoryCache;

        public MemoryCacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public bool GetMemoryCache(string cacheKey, out string? returnValue)
        {
            if (_memoryCache.TryGetValue(cacheKey, out string? tokenCache))
            {
                returnValue = tokenCache;
                return tokenCache != null;
            }

            returnValue = null;
            return false;
        }

        public void SetMemoryCache(string cacheKey, string value)
            => _memoryCache.Set(cacheKey, value);
    }
}
