using Microsoft.Extensions.Options;
using Orcamentaria.Lib.Domain.DTOs;
using Orcamentaria.Lib.Domain.DTOs.Request;
using Orcamentaria.Lib.Domain.Enums;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Models.Configurations;
using Orcamentaria.Lib.Domain.Services;

namespace Orcamentaria.Lib.Application.Services
{
    public class ApiGetawayService : IApiGetawayService
    {
        private readonly IHttpClientService _httpClientService;
        private readonly ApiGetawayConfiguration _apiGetawayConfiguration;

        public ApiGetawayService(
            IHttpClientService httpClientService,
            IOptions<ApiGetawayConfiguration> apiGetawayConfiguration)
        {
            _httpClientService = httpClientService;
            _apiGetawayConfiguration = apiGetawayConfiguration.Value;
        }

        public ApiGetawayConfiguration? GetResource(string serviceName, string endpointName)
        {
            var resource = _apiGetawayConfiguration.Resources
                .FirstOrDefault(x => x.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase) &&
                x.EndpointName.Equals(endpointName, StringComparison.OrdinalIgnoreCase));

            if (resource is null)
                return null;

            return new ApiGetawayConfiguration
            {
                BaseUrl = _apiGetawayConfiguration.BaseUrl,
                Resources = new List<ResourceConfiguration> { resource }
            };
        }

        public async Task<Response<T>> Routing<T>(
            string baseUrl,
            string serviceName,
            string endpointName,
            string token,
            IDictionary<string, string>? @params,
            object? payload)
        {
            if (!baseUrl.EndsWith("/"))
                baseUrl = $"{baseUrl}/";

            var endpoint = new EndpointRequest
            {
                Name = "Routing",
                Method = "POST",
                Route = "api/v1/Routing"
            };

            var content = new RequestDTO(serviceName, endpointName, @params, payload);

            var response = await _httpClientService.SendAsync<Response<T>>(
                    baseUrl: baseUrl,
                    endpoint: endpoint,
                    token,
                    content);

            if(!response.Success)
                return new Response<T>((ErrorCodeEnum)response.StatusCode, response.MessageError);

            return response.Content;
        }
    }
}
