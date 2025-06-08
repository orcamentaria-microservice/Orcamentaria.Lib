namespace Orcamentaria.Lib.Domain.Models
{
    public class HttpResponse<T>
    {
        public HttpResponseMessage HttpResponseMessage { get; set; }
        public EndpointRequest Endpoint { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public T Content { get; set; }
    }
}
