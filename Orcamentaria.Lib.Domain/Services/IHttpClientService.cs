using Orcamentaria.Lib.Domain.Models;

namespace Orcamentaria.Lib.Domain.Services
{
    public interface IHttpClientService
    {
        Task<HttpResponse<T>> SendAsync<T>(
           string baseUrl,
           EndpointRequest endpoint,
           OptionsRequest? options = null);
    }
}
