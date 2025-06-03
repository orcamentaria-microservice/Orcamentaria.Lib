using Orcamentaria.Lib.Domain.Enums;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Providers;
using Orcamentaria.Lib.Domain.Services;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orcamentaria.Lib.Domain.Models.Configurations;

namespace Orcamentaria.Lib.Application.Services
{
    public class ServiceRegistryService : IServiceRegistryService
    {
        private static string TOKEN_KEY = "_tokenRegistry_";

        private readonly IServer _server;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IOptions<ServiceRegistryConfiguration> _serviceRegistryConfiguration;
        private readonly HttpClient _httpClient;
        private readonly IMemoryCacheService _memoryCacheService;

        public ServiceRegistryService(
            IServer server, 
            IServiceScopeFactory scopeFactory,
            IHostApplicationLifetime lifetime, 
            IOptions<ServiceRegistryConfiguration> serviceRegistryConfiguration, 
            HttpClient httpClient, IMemoryCacheService memoryCacheService)
        {
            _server = server;
            _scopeFactory = scopeFactory;
            _lifetime = lifetime;
            _serviceRegistryConfiguration = serviceRegistryConfiguration;
            _httpClient = httpClient;
            _memoryCacheService = memoryCacheService;
        }

        public async Task<Response<dynamic>> SendAsync(
            string url, dynamic content, HttpMethod method, bool forceTokenGeneration = false)
        {
            if (forceTokenGeneration || !_memoryCacheService.GetMemoryCache(TOKEN_KEY, out string? bearerTokenServiceRegistry))
            {
                using var scope = _scopeFactory.CreateScope();
                var tokenProvider = scope.ServiceProvider.GetRequiredService<ITokenProvider>();
                bearerTokenServiceRegistry = await tokenProvider.GetTokenAsync();
                _memoryCacheService.SetMemoryCache(TOKEN_KEY, bearerTokenServiceRegistry);

                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerTokenServiceRegistry}");
            }

            var requestMessage = new HttpRequestMessage
            {
                Method = method,
                RequestUri = new Uri(url),
            };

            if (content is not null)
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                };

                requestMessage.Content = new StringContent(JsonSerializer.Serialize(content, options), Encoding.UTF8, "application/json");
            }

            try
            {
                var response = await _httpClient.SendAsync(requestMessage);

                if (!response.EnsureSuccessStatusCode().IsSuccessStatusCode)
                    throw new Exception();

                var contentResponse = JsonSerializer.Deserialize<Response<dynamic>>(await response.Content.ReadAsStringAsync());

                if (!forceTokenGeneration && 
                    !contentResponse.Success && 
                    contentResponse.Error.ErrorCode == ResponseErrorEnum.AccessDenied)
                    return SendAsync(url, content, method, true);

                return contentResponse;
            }
            catch (Exception ex)
            {
                return new Response<dynamic>(ResponseErrorEnum.ExternalServiceFailure, ex.Message);
            }
        }
    }
}
