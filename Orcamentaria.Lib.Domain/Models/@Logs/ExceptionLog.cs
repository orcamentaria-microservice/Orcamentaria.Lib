using System.Text.Json;

namespace Orcamentaria.Lib.Domain.Models.Logs
{
    public class ExceptionLog
    {
        public string TraceId { get; set; }
        public string TraceOrderId { get; set; }
        public ExceptionOrigin Origin { get; set; }
        public string Type { get; set; }
        public string Severity { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
        public PlaceException Place { get; set; }
        public DateTime Date { get; set; }
    }
}
