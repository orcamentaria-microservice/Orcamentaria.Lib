using Microsoft.Extensions.Configuration;
using Orcamentaria.Lib.Domain.DTOs.Authentication;
using Orcamentaria.Lib.Domain.Exceptions;
using Orcamentaria.Lib.Domain.Models.Configurations;
using Orcamentaria.Lib.Domain.Providers;
using Orcamentaria.Lib.Domain.Services;

namespace Orcamentaria.Lib.Application.Providers
{
    public class BootstrapTokenProvider : ITokenProvider
    {
        private readonly string _baseUrlApiGetaway;
        private readonly IApiGetawayService _apiGetawayService;
        private readonly IConfiguration _configuration;

        public BootstrapTokenProvider(
            string baseUrlApiGetaway,
            IApiGetawayService apiGetawayService,
            IConfiguration configuration)
        {
            _baseUrlApiGetaway = baseUrlApiGetaway;
            _apiGetawayService = apiGetawayService;
            _configuration = configuration;
        }

        public async Task<string> GetTokenAsync()
        {
            try
            {
                var resource = new ResourceConfiguration
                {
                    ServiceName = "AuthService",
                    EndpointName = "AuthenticateBootstrap",
                };

                IDictionary<string, string> @params = new Dictionary<string, string>();

                var bootstrapSecret = _configuration.GetSection("BOOTSTRAPSECRET");

                @params.Add("bootstrapSecret", bootstrapSecret.Value);

                var response = await _apiGetawayService.Routing<AuthenticationServiceResponseDTO>(
                    _baseUrlApiGetaway,
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
