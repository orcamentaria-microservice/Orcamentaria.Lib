using Microsoft.IdentityModel.Tokens;

namespace Orcamentaria.Lib.Domain.Services
{
    public interface IRsaService
    {
        RsaSecurityKey GenerateRsaSecurityKey(string projectName, string keyName);
    }
}
