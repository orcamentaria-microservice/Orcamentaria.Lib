using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orcamentaria.Lib.Domain.DTOs.ServiceRegistry;
using Orcamentaria.Lib.Domain.Enums;
using Orcamentaria.Lib.Domain.Exceptions;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Models.Configurations;
using Orcamentaria.Lib.Domain.Models.Responses;
using Orcamentaria.Lib.Domain.Providers;
using Orcamentaria.Lib.Domain.Services;

namespace Orcamentaria.Lib.Application.Services
{
    public class ServiceRegistryService : IServiceRegistryService
    {
        private static string TOKEN_KEY = "_tokenRegistry_";

        private readonly IApiGetawayService _apiGetawayService;
        private readonly ApiGetawayConfiguration _apiGetawayConfiguration;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHttpClientService _httpClientService;
        private readonly IMemoryCacheService _memoryCacheService;

        public ServiceRegistryService(
            IApiGetawayService apiGetawayService,
            IOptions<ApiGetawayConfiguration> apiGetawayConfiguration,
            IServiceScopeFactory scopeFactory,
            IHttpClientService httpClientService,
            IMemoryCacheService memoryCacheService)
        {
            _apiGetawayService = apiGetawayService;
            _apiGetawayConfiguration = apiGetawayConfiguration.Value;
            _scopeFactory = scopeFactory;
            _httpClientService = httpClientService;
            _memoryCacheService = memoryCacheService;
        }

        public async Task<Response<Task>> Heartbeat(string serviceId, bool forceTokenGeneration = false)
        {
            try
            {
                var tokenAuth = await GetTokenAsync(forceTokenGeneration);

                IDictionary<string, string> @params = new Dictionary<string, string>();

                @params.Add("serviceId", serviceId);

                var resource = new ResourceConfiguration
                {
                    ServiceName = "ServiceRegistry",
                    EndpointName = "ServiceHeartbeat",
                    Params = new List<string> { "serviceId" }
                };

                var response = await _apiGetawayService.Routing<Task>(
                        _apiGetawayConfiguration.BaseUrl,
                        resource.ServiceName,
                        resource.EndpointName,
                        tokenAuth,
                        @params,
                        null);

                if (!response.Success)
                    throw new IntegrationException("Falha ao enviar heartbeat para o Service Registry.", (System.Net.HttpStatusCode)response.Error.ErrorCode);

                return response;
            }
            catch (DefaultException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new UnexpectedException(ex.Message, ex);
            }
        }

        public async Task<Response<string>> Register(ServiceRegistryInsertDTO dto)
        {
            try
            {
                var tokenAuth = await GetTokenAsync(true);

                var resource = new ResourceConfiguration
                {
                    ServiceName = "ServiceRegistry",
                    EndpointName = "ServiceRegister"
                };

                var response = await _apiGetawayService.Routing<string>(
                    _apiGetawayConfiguration.BaseUrl,
                    resource.ServiceName,
                    resource.EndpointName,
                    tokenAuth,
                    new Dictionary<string, string>(),
                    dto);

                if (!response.Success)
                    throw new IntegrationException("Falha ao registrar o serviço no Service Registry.", (System.Net.HttpStatusCode)response.Error.ErrorCode);

                return response;
            }
            catch (DefaultException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new UnexpectedException(ex.Message, ex);
            }
        }

        public async Task<Response<T>> SendServiceRegister<T>(
            string baseUrl,
            ServiceRegistryConfigurationEndpoint endpoint,
            object content,
            bool forceTokenGeneration = false)
        {
            try
            {
                var tokenAuth = "";

                if (endpoint.RequiredAuthorization)
                {
                    tokenAuth = await GetTokenAsync(forceTokenGeneration);
                }

                var response = await _httpClientService.SendAsync<T>(
                    baseUrl: baseUrl,
                    endpoint: endpoint,
                    options: new OptionsRequest
                    {
                        TokenAuth = tokenAuth,
                        Content = content
                    });

                if (!forceTokenGeneration &&
                    !response.Content.Success &&
                    response.Content.Error.ErrorCode == ErrorCodeEnum.Unauthorized)
                    return await SendServiceRegister<T>(baseUrl, endpoint, content, true);

                return response.Content;
            }
            catch (DefaultException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new UnexpectedException(ex.Message, ex);
            }
        }

        #region Private Methods
        private async Task<string> GetTokenAsync(bool forceTokenGeneration = false)
        {
            try
            {
                if (forceTokenGeneration || !_memoryCacheService.GetMemoryCache(TOKEN_KEY, out string? tokenService))
                {
                    using var scope = _scopeFactory.CreateScope();
                    var tokenProvider = scope.ServiceProvider.GetRequiredService<ITokenProvider>();
                    tokenService = await tokenProvider.GetTokenAsync();

                    if (String.IsNullOrWhiteSpace(tokenService))
                        throw new UnexpectedException("Falha ao gerar o token.", ErrorCodeEnum.InternalError);

                    _memoryCacheService.SetMemoryCache(TOKEN_KEY, tokenService);
                }
                return tokenService;
            }
            catch (DefaultException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new UnexpectedException(ex.Message, ex);
            }
        }

        #endregion

    }
}
