using Orcamentaria.Lib.Domain.Enums;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Providers;
using Orcamentaria.Lib.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Orcamentaria.Lib.Domain.Models.Configurations;

namespace Orcamentaria.Lib.Application.Services
{
    public class ServiceRegistryService : IServiceRegistryService
    {
        private static string TOKEN_KEY = "_tokenRegistry_";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHttpClientService _httpClientService;
        private readonly IMemoryCacheService _memoryCacheService;

        public ServiceRegistryService(
            IServiceScopeFactory scopeFactory,
            IHttpClientService httpClientService, 
            IMemoryCacheService memoryCacheService)
        {
            _scopeFactory = scopeFactory;
            _httpClientService = httpClientService;
            _memoryCacheService = memoryCacheService;
        }

        public async Task<Response<T>> SendServiceRegister<T>(
            string baseUrl, 
            ServiceRegistryConfigurationEndpoint endpoint,
            object content,
            bool forceTokenGeneration = false)
        {
            var tokenAuth = "";

            if(endpoint.RequiredAuthorization)
            {
                if (forceTokenGeneration || !_memoryCacheService.GetMemoryCache(TOKEN_KEY, out string? tokenService))
                {
                    using var scope = _scopeFactory.CreateScope();
                    var tokenProvider = scope.ServiceProvider.GetRequiredService<ITokenProvider>();
                    tokenService = await tokenProvider.GetTokenServiceAsync();

                    if (String.IsNullOrWhiteSpace(tokenService))
                        throw new Exception("Token não gerado.");

                    tokenAuth = tokenService;
                    _memoryCacheService.SetMemoryCache(TOKEN_KEY, tokenService);
                }
            }

            try
            {
                var response = await _httpClientService.SendAsync<Response<T>>(baseUrl, endpoint, tokenAuth, content);

                if (!response.Success)
                    throw new Exception();

                if (!forceTokenGeneration && 
                    !response.Content.Success &&
                    response.Content.Error.ErrorCode == ResponseErrorEnum.AccessDenied)
                    return await SendServiceRegister<T>(baseUrl, endpoint, content, true);

                return response.Content;
            }
            catch (Exception ex)
            {
                return new Response<T>(ResponseErrorEnum.ExternalServiceFailure, ex.Message);
            }
        }
    }
}
