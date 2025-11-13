using Orcamentaria.Lib.Domain.Models.Configurations;

namespace Orcamentaria.Lib.Domain.DTOs.Request
{
    public class RequestDTO
    {
        public string ServiceName { get; set; }
        public string EndpointName { get; set; }
        public IEnumerable<RequestParamDTO>? Params { get; set; }
        public object? Content { get; set; }

        public RequestDTO() { }

        public RequestDTO(
            string serviceName,
            string endpointName,
            IDictionary<string, string>? @params,
            object? content)
        {
            ServiceName = serviceName;
            EndpointName = endpointName;
            Params = @params?.Select(x => new RequestParamDTO { ParamName = x.Key, ParamValue = x.Value });
            Content = content;
        }
    }
}
