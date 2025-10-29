﻿namespace Orcamentaria.Lib.Domain.Services
{
    public interface IMessageBrokerConsumerService
    {
        Task HandleBasicDeliverAsync(
            string queueConsume, 
            CancellationToken stoppingToken, 
            Func<string, Task> processMessage);
    }
}
