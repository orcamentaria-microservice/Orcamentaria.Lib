namespace Orcamentaria.Lib.Domain.DTOs.ServiceRegistry
{
    public class ServiceRegistryInsertDTO
    {
        public string Name { get; set; }
        public string BaseUrl { get; set; }
        public List<ServiceRegistryEndpoinsInsertDTO> Endpoints { get; set; }
    }
}
