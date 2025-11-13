using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orcamentaria.Lib.Application.HostedServices;
using Orcamentaria.Lib.Domain.Models.Configurations;

namespace Orcamentaria.Lib.Infrastructure.Configures
{
    public static class ServiceRegistryHostedConfigure
    {
        public static void AddServiceRegistryHosted(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            if (configuration.GetSection("ServiceConfiguration") is null)
                throw new Exception("Serviço não configurado.");

            services.Configure<ServiceConfiguration>(configuration.GetSection("ServiceConfiguration"));
            services.AddHostedService<ServiceRegistryHostedService>();
        }
    }
}
