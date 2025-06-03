namespace Orcamentaria.Lib.Domain.HostedService
{
    public interface IServiceRegistryHostedService
    {
        Task SendRegisterService();
        void SendHeartbeat(object state);
    }
}
