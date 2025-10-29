using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orcamentaria.Lib.Domain.Services;

namespace Orcamentaria.Lib.Application.HostedServices
{
    public class RealTimeConfigurationHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public RealTimeConfigurationHostedService(IServiceScopeFactory scopeFactory)
            => _scopeFactory = scopeFactory;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var serviceName = scope.ServiceProvider.GetRequiredService<string>();
                var topologyBroker = scope.ServiceProvider.GetRequiredService<ITopologyBrokerService>();
                var consumer = scope.ServiceProvider.GetRequiredService<IMessageBrokerConsumerService>();
                var processor = scope.ServiceProvider.GetRequiredKeyedService<IMessageBrokerProcessorService>(serviceName);

               await  topologyBroker.CreateTopicExchangeAsync(
                    exchange: "realTimeConfiguration", 
                    binds: [serviceName]);

                await consumer.HandleBasicDeliverAsync(
                    queueConsume: $"realTimeConfiguration.{serviceName}",
                    stoppingToken: stoppingToken,
                    processMessage: processor.ProcessAsync);
            }
        }
    }
}
