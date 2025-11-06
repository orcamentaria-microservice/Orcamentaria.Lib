using Orcamentaria.Lib.Domain.Models.Configurations;
using Orcamentaria.Lib.Domain.Models.Responses;

namespace Orcamentaria.Lib.Domain.Services
{
    public interface IApiGetawayService
    {
        Task<Response<T>> Routing<T>(
            string baseUrl,
            string serviceName, 
            string endpointName,
            string token,
            IDictionary<string, string>? @params,
            object? payload);

        ApiGetawayConfiguration GetApiGetawayConfiguration(string serviceName, string endpointName);
    }
}
