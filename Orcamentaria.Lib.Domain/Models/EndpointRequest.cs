using System.Text.Json.Serialization;

namespace Orcamentaria.Lib.Domain.Models
{
    public class EndpointRequest
    {
        public required string Name { get; set; }
        public required string Route { get; set; }
        public required string Method { get; set; }

        [JsonIgnore]
        public int Order { get; set; } = 0;

    }
}
