using Orcamentaria.Lib.Domain.Enums;

namespace Orcamentaria.Lib.Domain.Models
{
    public class ServiceRegister
    {
        public string Id { get; set; }
        public string ServiceName { get; set; }
        public string EndpointName { get; set; }
        public string BaseUrl { get; set; }
        public string Route { get; set; }
        public string Method { get; set; }
        public ServiceStateEnum State { get; set; }

        public ServiceRegister() { }
    }
}
