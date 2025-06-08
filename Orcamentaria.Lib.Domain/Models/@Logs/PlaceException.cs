namespace Orcamentaria.Lib.Domain.Models.Logs
{
    public class PlaceException
    {
        public string ServiceName { get; set; }
        public string ProjectName { get; set; }
        public string FunctionName { get; set; }
        public int Line { get; set; }
    }
}
