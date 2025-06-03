using Orcamentaria.Lib.Domain.Providers;

namespace Orcamentaria.Lib.Application.Providers
{
    public class TokenProvider : ITokenProvider
    {
        public Task<string> GetTokenAsync()
        {
            return Task.FromResult("sem token");
        }
    }
}
