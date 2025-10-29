using Microsoft.Extensions.Options;
using Orcamentaria.Lib.Domain.Models.Configurations;
using Orcamentaria.Lib.Domain.Services;
using RabbitMQ.Client;
using System.Text;

namespace Orcamentaria.Lib.Application.Services
{
    public class RabbitMqPublishService : IPublishMessageBrokerService
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly ITopologyBrokerService _topologyBrokerService;

        public RabbitMqPublishService(
            ITopologyBrokerService topologyBrokerService,
            IOptions<MessageBrokerConfiguration> messageBrokerConfiguration)
        {
            _topologyBrokerService = topologyBrokerService;
            var factory = new ConnectionFactory {
                HostName = messageBrokerConfiguration.Value.Host,
                Port = messageBrokerConfiguration.Value.Port,
                UserName = messageBrokerConfiguration.Value.UserName,
                Password = messageBrokerConfiguration.Value.Password
            };
            _connection = factory.CreateConnectionAsync().Result;
            _channel = _connection.CreateChannelAsync().Result;
        }

        public async Task SendMessageToTopicExchange(string message, string exchange, string routingKey, string[] binds)
        {
            var bytes = Encoding.UTF8.GetBytes(message);

            await _topologyBrokerService.CreateTopicExchangeAsync(exchange, binds);

            await _channel.BasicPublishAsync(exchange: exchange, routingKey: routingKey, body: bytes);
        }

        public async Task SendMessageToQueue(string message, string queue)
        {
            var bytes = Encoding.UTF8.GetBytes(message);

            await _channel.QueueDeclareAsync(
                queue: queue, 
                durable: true, 
                exclusive: false, 
                autoDelete: false,
                arguments: null);

            await _channel.BasicPublishAsync(exchange: string.Empty, routingKey: queue, body: bytes);
        }
    }
}
