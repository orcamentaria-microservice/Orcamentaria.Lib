namespace Orcamentaria.Lib.Domain.Models.Configurations
{
    public class MessageBrokerConfiguration
    {
        public string BrokerName { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
