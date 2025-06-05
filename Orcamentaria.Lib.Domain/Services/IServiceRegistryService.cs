using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Models.Configurations;

namespace Orcamentaria.Lib.Domain.Services
{
    public interface IServiceRegistryService
    {
        Task<Response<T>> SendServiceRegister<T>(
            string baseUrl, 
            ServiceRegistryConfigurationEndpoint endpoint, 
            object? content = null,
            bool forceTokenGeneration = false);
    }
}
