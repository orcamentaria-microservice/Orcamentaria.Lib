namespace Orcamentaria.Lib.Domain.DTOs
{
    public class ServiceRegistryInsertDTO
    {
        public string Name { get; set; }
        public string BaseUrl { get; set; }
        public List<ServiceRegistryEndpoinsInsertDTO> Endpoints { get; set; }
    }
}
