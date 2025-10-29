namespace Orcamentaria.Lib.Domain.Services
{
    public interface ITopologyBrokerService
    {
        Task CreateTopicExchangeAsync(string exchange, string[] binds);
    }
}
