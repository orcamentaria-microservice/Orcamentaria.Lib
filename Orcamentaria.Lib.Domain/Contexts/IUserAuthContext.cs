namespace Orcamentaria.Lib.Domain.Contexts
{
    public interface IUserAuthContext
    {
        public long UserId { get; set; }
        public string Email { get; set; }
        public long CompanyId { get; set; }
        public string BearerToken { get; set; }
    }
}
