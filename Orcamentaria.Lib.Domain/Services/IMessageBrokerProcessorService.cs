namespace Orcamentaria.Lib.Domain.Services
{
    public interface IMessageBrokerProcessorService
    {
        Task ProcessAsync(string message);
    }
}
