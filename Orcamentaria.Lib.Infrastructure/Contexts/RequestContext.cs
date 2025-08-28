using Orcamentaria.Lib.Domain.Contexts;

namespace Orcamentaria.Lib.Infrastructure.Contexts
{
    public class RequestContext : IRequestContext
    {
        public Guid RequestId { get; set; }
        public int RequestOrderId { get; set; }
    }
}
