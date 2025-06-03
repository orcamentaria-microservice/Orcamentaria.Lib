namespace Orcamentaria.Lib.Domain.Models.Configurations
{
    public class ServiceRegistryConfiguration
    {
        public string ServiceName { get; set; }
        public string BaseUrl { get; set; }
        public string RegistryServiceRoute { get; set; }
        public string HeartbeatRoute { get; set; }
    }
}
