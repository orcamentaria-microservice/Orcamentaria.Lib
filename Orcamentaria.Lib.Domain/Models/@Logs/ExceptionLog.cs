namespace Orcamentaria.Lib.Domain.Models.Logs
{
    public class ExceptionLog
    {
        public ExceptionOrigin Origin { get; set; }
        public string Type { get; set; }
        public string Severity { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
        public PlaceException Place { get; set; }
        public DateTime Date { get; set; }
    }
}
