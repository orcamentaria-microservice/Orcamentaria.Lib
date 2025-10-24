namespace Orcamentaria.Lib.Domain.Providers
{
    public interface ITokenProvider
    {
        public Task<string> GetTokenAsync();
    }
}
