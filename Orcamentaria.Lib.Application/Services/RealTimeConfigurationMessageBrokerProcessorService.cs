using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Orcamentaria.Lib.Domain.Exceptions;
using Orcamentaria.Lib.Domain.Services;
using System.Text.Json;

namespace Orcamentaria.Lib.Application.Services
{
    public class RealTimeConfigurationMessageBrokerProcessorService : IMessageBrokerProcessorService
    {
        private readonly MemoryConfigurationProvider _mem;
        private readonly IConfigurationRoot _configuration;

        public RealTimeConfigurationMessageBrokerProcessorService(
            MemoryConfigurationProvider mem,
            IConfiguration configuration)
        {
            _mem = mem;
            _configuration = (IConfigurationRoot)configuration;
        }

        public Task ProcessAsync(string message)
        {
            try
            {
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var realTimeConfigs = JsonSerializer.Deserialize<IEnumerable<Dictionary<string, string>>>(message);

                foreach (var row in realTimeConfigs)
                {
                    if (row is null) continue;

                    var key = row.Keys.First();
                    var value = row.Values.First();

                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                    {
                        _mem.Set($"RealTimeConfigurations:{key}", value);
                        continue;
                    }
                }

                _configuration.Reload();
                return Task.CompletedTask;
            }
            catch (DefaultException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new UnexpectedException(ex.Message, ex);
            }
        }
    }
}
