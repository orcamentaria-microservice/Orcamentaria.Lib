using Orcamentaria.Lib.Domain.Models;

namespace Orcamentaria.Lib.Domain.Services
{
    public interface IServiceRegistryService
    {
        Task<Response<dynamic>> SendAsync(string url, dynamic content, HttpMethod method, bool forceTokenGeneration = false);
    }
}
