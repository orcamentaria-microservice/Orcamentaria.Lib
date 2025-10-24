namespace Orcamentaria.Lib.Domain.DTOs.ConfigurationBag
{
    public class ConfigurationBagResponseDTO
    {
        public string ServiceName { get; set; }
        public DateTime UpdateAt { get; set; }
        public IEnumerable<Dictionary<string, string>> ConnectionStrings { get; set; } = [];
        public IEnumerable<Dictionary<string, object>> Configurations { get; set; }
    }
}
