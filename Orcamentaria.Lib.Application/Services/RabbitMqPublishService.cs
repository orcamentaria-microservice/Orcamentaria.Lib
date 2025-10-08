using Orcamentaria.Lib.Domain.Services;
using RabbitMQ.Client;
using System.Text;

namespace Orcamentaria.Lib.Application.Services
{
    public class RabbitMqPublishService : IPublishMessageBrokerService
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;

        public RabbitMqPublishService(string host)
        {
            var factory = new ConnectionFactory { HostName = host };
            _connection = factory.CreateConnectionAsync().Result;
            _channel = _connection.CreateChannelAsync().Result;
        }

        public async Task SendMessageToTopicExchange(string message, string exchange, string routingKey, string[] binds)
        {
            var bytes = Encoding.UTF8.GetBytes(message);

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
