using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orcamentaria.Lib.Infrastructure.Initializers;

namespace Orcamentaria.Lib.Infrastructure.Configures
{
    public static class EnvsConfigure
    {
        public static IConfiguration ResolveConfigs(
            this IServiceCollection services,
            IConfiguration configuration,
            string serviceName)
        {
            var newConfigs = new ConfigurationBagInitializer(serviceName)
                .InitializeAsync(configuration)
                .GetAwaiter()
                .GetResult();

            services.Replace(ServiceDescriptor.Singleton<IConfiguration>(newConfigs));

            return newConfigs;
        }
    }
}
