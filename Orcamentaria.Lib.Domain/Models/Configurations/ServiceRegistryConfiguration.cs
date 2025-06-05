namespace Orcamentaria.Lib.Domain.Models.Configurations
{
    public class ServiceRegistryConfiguration : ApiConfiguration
    {
        public required IEnumerable<ServiceRegistryConfigurationEndpoint> Endpoints { get; set; }
    }
}
