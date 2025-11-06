using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Models.Responses;

namespace Orcamentaria.Lib.Domain.Services
{
    public interface IHttpClientService
    {
        Task<HttpResponse<Response<T>>> SendAsync<T>(
           string baseUrl,
           EndpointRequest endpoint,
           OptionsRequest? options = null);
    }
}
