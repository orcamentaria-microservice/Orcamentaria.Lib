namespace Orcamentaria.Lib.Domain.Contexts
{
    public interface IUserAuthContext
    {
        public long UserId { get; set; }
        public string UserEmail { get; set; }
        public long UserCompanyId { get; set; }
        public string UserToken { get; set; }
    }
}
