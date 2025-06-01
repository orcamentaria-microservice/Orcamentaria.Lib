namespace Orcamentaria.Lib.Domain.Models
{
    public class UserAuth
    {
        public long Id { get; set; }
        public string Email { get; set; }
        public long CompanyId { get; set; }
    }
}
