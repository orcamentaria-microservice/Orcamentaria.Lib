using Orcamentaria.Lib.Domain.Contexts;

namespace Orcamentaria.Lib.Infrastructure.Contexts
{
    public class ServiceAuthContext : IServiceAuthContext
    {
        public long ServiceId { get; set; }
        public string Name { get; set; }
        public string BearerToken { get; set; }
    }
}
