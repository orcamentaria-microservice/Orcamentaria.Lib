using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace Orcamentaria.Lib.Domain.Services
{
    public interface IRsaService
    {
        RsaSecurityKey GenerateRsaSecurityKey(string projectName, string keyName);
    }
}
