namespace Orcamentaria.Lib.Domain.Services
{
    public interface IMemoryCacheService
    {
        void SetMemoryCache(string cacheKey, string value);
        bool GetMemoryCache(string cacheKey, out string? returnValue);
    }
}
