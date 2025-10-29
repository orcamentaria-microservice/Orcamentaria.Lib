using Microsoft.Extensions.Options;
using Orcamentaria.Lib.Domain.Exceptions;
using Orcamentaria.Lib.Domain.Models.Configurations;
using Orcamentaria.Lib.Domain.Services;
using RabbitMQ.Client;

namespace Orcamentaria.Lib.Application.Services
{
    public class RabbitMqTopologyBrokerService : ITopologyBrokerService
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        public RabbitMqTopologyBrokerService(
            IOptions<MessageBrokerConfiguration> messageBrokerConfiguration)
        {
            var factory = new ConnectionFactory
            {
                HostName = messageBrokerConfiguration.Value.Host,
                Port = messageBrokerConfiguration.Value.Port,
                UserName = messageBrokerConfiguration.Value.UserName,
                Password = messageBrokerConfiguration.Value.Password
            };
            _connection = factory.CreateConnectionAsync().Result;
            _channel = _connection.CreateChannelAsync().Result;
        }

        public async Task CreateTopicExchangeAsync(string exchange, string[] binds)
        {
			try
			{
                await _channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic);

                foreach (var bind in binds)
                {
                    await _channel.QueueDeclareAsync(
                    queue: $"{exchange}.{bind}".Replace(".*", ""),
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                    await _channel.QueueBindAsync(
                    queue: $"{exchange}.{bind}".Replace(".*", ""),
                    exchange: exchange,
                    routingKey: $"{exchange}.{bind}");
                }
            }
			catch (Exception ex)
			{
                throw new UnexpectedException(ex.Message, ex);
            }
        }
    }
}
