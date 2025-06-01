using Orcamentaria.Lib.Domain.Contexts;

namespace Orcamentaria.Lib.Infrastructure.Contexts
{
    public class CompanyContext : ICompanyContext
    {
        public long CompanyId { get; set; }
    }
}
