using Microsoft.Extensions.Options;
using Orcamentaria.Lib.Domain.DTOs.Request;
using Orcamentaria.Lib.Domain.Enums;
using Orcamentaria.Lib.Domain.Exceptions;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Models.Configurations;
using Orcamentaria.Lib.Domain.Models.Exceptions;
using Orcamentaria.Lib.Domain.Models.Responses;
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

        public ApiGetawayConfiguration GetApiGetawayConfiguration(string serviceName, string endpointName)
        {
            try
            {
                if (!_apiGetawayConfiguration.Resources.Any())
                    throw new InfoException("Nenhum recurso configurado para API Getaway", ErrorCodeEnum.NotFound);

                var resource = _apiGetawayConfiguration.Resources
                    .FirstOrDefault(x => x.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase) &&
                    x.EndpointName.Equals(endpointName, StringComparison.OrdinalIgnoreCase));

                if (resource is null)
                    throw new InfoException($"Não encontrado nenhum recurso para {serviceName} e {endpointName}.", ErrorCodeEnum.NotFound);

                return new ApiGetawayConfiguration
                {
                    BaseUrl = _apiGetawayConfiguration.BaseUrl,
                    Resources = new List<ResourceConfiguration> { resource }
                };
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

        public async Task<Response<T>> Routing<T>(
            string baseUrl,
            string serviceName,
            string endpointName,
            string token,
            IDictionary<string, string>? @params,
            object? payload)
        {
            try
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

                var response = await _httpClientService.SendAsync<T>(
                        baseUrl: baseUrl,
                        endpoint: endpoint,
                        options: new OptionsRequest
                        {
                            TokenAuth = token,
                            Content = content
                        });

                if (!response.Content.Success)
                    throw new UnexpectedException(
                        String.Join(" || ", response.Content.Error.MessageErrors.Select(x => x.Message)), 
                        response.Content.Error.ErrorCode, 
                        SeverityLevelEnum.Warning);

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
    }
}
