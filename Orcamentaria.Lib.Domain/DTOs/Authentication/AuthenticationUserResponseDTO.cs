namespace Orcamentaria.Lib.Domain.DTOs.Authentication
{
    public class AuthenticationUserResponseDTO
    {
        public long UserId { get; set; }
        public string Name { get; set; }
        public long CompanyId { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
    }
}
