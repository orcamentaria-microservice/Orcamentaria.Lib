using Orcamentaria.Lib.Domain.Contexts;

namespace Orcamentaria.Lib.Infrastructure.Contexts
{
    public class ServiceAuthContext : IServiceAuthContext
    {
        public long ServiceId { get; set; }
        public string ServiceName { get; set; }
        public string ServiceToken { get; set; }
    }
}
