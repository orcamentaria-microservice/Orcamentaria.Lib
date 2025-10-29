using Microsoft.Extensions.Options;
using Orcamentaria.Lib.Domain.DTOs.Authentication;
using Orcamentaria.Lib.Domain.Exceptions;
using Orcamentaria.Lib.Domain.Models.Configurations;
using Orcamentaria.Lib.Domain.Providers;
using Orcamentaria.Lib.Domain.Services;

namespace Orcamentaria.Lib.Application.Providers
{
    public class ServiceTokenProvider : ITokenProvider
    {
        private readonly ServiceConfiguration _serviceConfiguration;
        private readonly IApiGetawayService _apiGetawayService;
        private readonly ApiGetawayConfiguration _apiGetawayConfiguration;

        public ServiceTokenProvider(
            IOptions<ServiceConfiguration> serviceConfiguration,
            IApiGetawayService apiGetawayService,
            IOptions<ApiGetawayConfiguration> apiGetawayConfiguration)
        {
            _serviceConfiguration = serviceConfiguration.Value;
            _apiGetawayService = apiGetawayService;
            _apiGetawayConfiguration = apiGetawayConfiguration.Value;
        }

        public async Task<string> GetTokenAsync()
        {
            try
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

                return response.Data.Token;
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
