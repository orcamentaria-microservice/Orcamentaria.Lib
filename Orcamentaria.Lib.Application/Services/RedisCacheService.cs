using Microsoft.Extensions.Caching.Distributed;
using Orcamentaria.Lib.Domain.Services;
using System.Text.Json;

namespace Orcamentaria.Lib.Application.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private static readonly JsonSerializerOptions _jsonOptions = new();

        public RedisCacheService(IDistributedCache cache) => _cache = cache;

        public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            var data = await _cache.GetStringAsync(key, ct);
            return data is null ? default : JsonSerializer.Deserialize<T>(data, _jsonOptions);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
        {
            var options = new DistributedCacheEntryOptions();
            if (expiration.HasValue)
                options.SetAbsoluteExpiration(expiration.Value);

            var data = JsonSerializer.Serialize(value, _jsonOptions);
            await _cache.SetStringAsync(key, data, options, ct);
        }

        public Task RemoveAsync(string key, CancellationToken ct = default)
            => _cache.RemoveAsync(key, ct);

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken ct = default)
        {
            var cached = await GetAsync<T>(key, ct);
            if (cached is not null)
                return cached;

            var value = await factory();
            await SetAsync(key, value, expiration, ct);
            return value;
        }
    }
}
