using Orcamentaria.Lib.Domain.Contexts;

namespace Orcamentaria.Lib.Infrastructure.Contexts
{
    public class RequestContext : IRequestContext
    {
        public string RequestId { get; set; }
        public int RequestOrderId { get; set; } = 0;
    }
}
