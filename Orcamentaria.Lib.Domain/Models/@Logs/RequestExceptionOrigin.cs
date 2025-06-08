namespace Orcamentaria.Lib.Domain.Models.Logs
{
    public class RequestExceptionOrigin  : ExceptionOrigin
    {
        public string Id { get; set; }
        public string Host { get; set; }
        public string Route { get; set; }
        public string Method { get; set; }
        public string Body { get; set; }
        public string Query { get; set; }
    }
}
