using Orcamentaria.Lib.Domain.Models.Configurations;

namespace Orcamentaria.Lib.Domain.HostedService
{
    public interface IServiceRegistryHostedService
    {
        Task SendRegisterService();
        Task SendHeartbeat();
    }
}
