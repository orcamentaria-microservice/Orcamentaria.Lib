using Orcamentaria.Lib.Domain.DTOs.ServiceRegistry;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Models.Configurations;

namespace Orcamentaria.Lib.Domain.Services
{
    public interface IServiceRegistryService
    {
        Task<Response<string>> Register(ServiceRegistryInsertDTO dto);
        Task<Response<Task>> Heartbeat(string serviceId, bool forceTokenGeneration = false);
        Task<Response<T>> SendServiceRegister<T>(
            string baseUrl,
            ServiceRegistryConfigurationEndpoint endpoint,
            object? content = null,
            bool forceTokenGeneration = false);
    }
}
