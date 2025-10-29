namespace Orcamentaria.Lib.Domain.Models
{
    public class ServiceRegistry
    {
        public string Id { get; set; }
        public int Order { get; set; }
        public string ServiceName { get; set; }
        public string BaseUrl { get; set; }
        public StateService State { get; set; }
        public IEnumerable<EndpointRequest> Endpoints { get; set; }

        public ServiceRegistry() { }
    }
}
