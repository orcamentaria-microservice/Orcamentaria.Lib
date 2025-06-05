namespace Orcamentaria.Lib.Domain.Models.Configurations
{
    public class ServiceRegistryConfigurationEndpoint : EndpointRequest
    {
        public bool RequiredAuthorization { get; set; } = true;
    }
}
