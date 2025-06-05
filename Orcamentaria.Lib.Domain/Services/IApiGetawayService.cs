using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Models.Configurations;

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

        ApiGetawayConfiguration? GetResource(string serviceName, string endpointName);
    }
}
