using Orcamentaria.Lib.Domain.Contexts;

namespace Orcamentaria.Lib.Infrastructure.Contexts
{
    public class UserAuthContext : IUserAuthContext
    {
        public long UserId { get; set; }
        public string UserEmail { get; set; }
        public long UserCompanyId { get; set; }
    }
}
