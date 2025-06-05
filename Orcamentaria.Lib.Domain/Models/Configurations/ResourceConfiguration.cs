namespace Orcamentaria.Lib.Domain.Models.Configurations
{
    public class ResourceConfiguration
    {
        public string ServiceName { get; set; }
        public string EndpointName { get; set; }
        public IEnumerable<string> Params { get; set; }
    }
}
