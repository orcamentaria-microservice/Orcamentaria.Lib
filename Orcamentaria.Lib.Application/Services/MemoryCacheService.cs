using Microsoft.Extensions.Caching.Memory;
using Orcamentaria.Lib.Domain.Exceptions;
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
            try
            {
                if (_memoryCache.TryGetValue(cacheKey, out string? tokenCache))
                {
                    returnValue = tokenCache;
                    return true;
                }

                returnValue = null;
                return false;
            }
            catch (Exception ex)
            {
                throw new UnexpectedException(ex.Message, ex);
            }
        }

        public void SetMemoryCache(string cacheKey, string value)
        {
            try
            {
                _memoryCache.Set(cacheKey, value);
            }
            catch (Exception ex)
            {
                throw new UnexpectedException(ex.Message, ex);
            }
        }
    }
}
