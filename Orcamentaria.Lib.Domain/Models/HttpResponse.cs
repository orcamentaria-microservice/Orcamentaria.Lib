namespace Orcamentaria.Lib.Domain.Models
{
    public class HttpResponse<T>
    {
        public bool Success { get; set; }
        public HttpResponseMessage HttpResponseMessage { get; set; }
        public EndpointRequest Endpoint { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public string? MessageError { get; set; } = null;
        public int StatusCode { get; set; } = 200;
        public T Content { get; set; }
    }
}
