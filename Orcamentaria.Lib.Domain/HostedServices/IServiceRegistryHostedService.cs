using Orcamentaria.Lib.Domain.Models.Configurations;

namespace Orcamentaria.Lib.Domain.HostedServices
{
    public interface IServiceRegistryHostedService
    {
        Task SendRegisterService();
        Task SendHeartbeat();
    }
}
