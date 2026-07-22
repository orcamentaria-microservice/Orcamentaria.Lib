using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orcamentaria.Lib.Application.Services;
using Orcamentaria.Lib.Domain.DTOs.Authentication;
using Orcamentaria.Lib.Domain.Enums;
using Orcamentaria.Lib.Domain.Exceptions;
using Orcamentaria.Lib.Domain.Models.Configurations;
using Orcamentaria.Lib.Domain.Providers;
using Orcamentaria.Lib.Domain.Services;

namespace Orcamentaria.Lib.Application.Providers
{
    public class ServiceTokenProvider : ITokenProvider
    {
        private static string TOKEN_KEY = "_tokenService_";
        private readonly ServiceConfiguration _serviceConfiguration;
        private readonly IApiGetawayService _apiGetawayService;
        private readonly ApiGetawayConfiguration _apiGetawayConfiguration;
        private readonly ICacheService _memoryCacheService;

        public ServiceTokenProvider(
            IOptions<ServiceConfiguration> serviceConfiguration,
            IApiGetawayService apiGetawayService,
            IOptions<ApiGetawayConfiguration> apiGetawayConfiguration,
            [FromKeyedServices("memory")] ICacheService memoryCacheService)
        {
            _serviceConfiguration = serviceConfiguration.Value;
            _apiGetawayService = apiGetawayService;
            _apiGetawayConfiguration = apiGetawayConfiguration.Value;
            _memoryCacheService = memoryCacheService;
        }

        public async Task<string> GetTokenAsync(bool forceTokenGeneration = false)
        {
            try
            {
                var tokenService = forceTokenGeneration ? null : await _memoryCacheService.GetAsync<string>(TOKEN_KEY);

                if (forceTokenGeneration || string.IsNullOrWhiteSpace(tokenService))
                {
                    var resource = new ResourceConfiguration
                    {
                        ServiceName = "AuthService",
                        EndpointName = "AuthenticateService",
                        Params = new List<string> { "clientId", "clientSecret" }
                    };

                    IDictionary<string, string> @params = new Dictionary<string, string>();

                    @params.Add("clientId", _serviceConfiguration.ClientId);
                    @params.Add("clientSecret", _serviceConfiguration.ClientSecret);

                    var response = await _apiGetawayService.Routing<AuthenticationServiceResponseDTO>(
                        _apiGetawayConfiguration.BaseUrl,
                        resource.ServiceName,
                        resource.EndpointName,
                        String.Empty,
                        @params,
                        null);

                    tokenService = response.Data.Token;

                    if (String.IsNullOrWhiteSpace(tokenService))
                        throw new UnexpectedException("Falha ao gerar o token.", ErrorCodeEnum.InternalError);

                    await _memoryCacheService.SetAsync(TOKEN_KEY, tokenService);
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
    }
}
