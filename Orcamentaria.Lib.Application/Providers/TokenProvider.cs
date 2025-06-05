using Microsoft.Extensions.Options;
using Orcamentaria.APIGetaway.Domain.DTOs.Authentication;
using Orcamentaria.Lib.Domain.Models.Configurations;
using Orcamentaria.Lib.Domain.Providers;
using Orcamentaria.Lib.Domain.Services;

namespace Orcamentaria.Lib.Application.Providers
{
    public class TokenProvider : ITokenProvider
    {
        private readonly ServiceConfiguration _serviceConfiguration;
        private readonly IApiGetawayService _apiGetawayService;

        public TokenProvider(
            IOptions<ServiceConfiguration> serviceConfiguration,
            IApiGetawayService apiGetawayService)
        {
            _serviceConfiguration = serviceConfiguration.Value;
            _apiGetawayService = apiGetawayService;
        }

        public async Task<string> GetTokenServiceAsync()
        {
            var apiGetawayConfiguration = _apiGetawayService.GetResource("AuthService", "ServiceAuthenticate");

            if (apiGetawayConfiguration is null)
                return String.Empty;

            var resource = apiGetawayConfiguration.Resources.First();

            IDictionary<string, string> @params = new Dictionary<string, string>();

            @params.Add("clientId", _serviceConfiguration.ClientId);
            @params.Add("clientSecret", _serviceConfiguration.ClientSecret);

            try
            {
                var response = await _apiGetawayService.Routing<AuthenticationServiceResponseDTO>(
                    apiGetawayConfiguration.BaseUrl,
                    resource.ServiceName,
                    resource.EndpointName,
                    String.Empty,
                    @params,
                    null);

                if (!response.Success)
                    return String.Empty;

                return response.Data.Token;
            }
            catch (Exception)
            {
                return String.Empty;
            }
        }
    }
}
