namespace Orcamentaria.Lib.Domain.Services
{
    public interface IPublishMessageBrokerService
    {
        Task SendMessageToQueue(string message, string queue);
        Task SendMessageToTopicExchange(string message, string exchange, string routingKey, string[] binds);
    }
}
