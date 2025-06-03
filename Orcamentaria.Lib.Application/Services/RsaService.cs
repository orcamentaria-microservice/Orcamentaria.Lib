using Microsoft.IdentityModel.Tokens;
using Orcamentaria.Lib.Domain.Services;
using System.Security.Cryptography;

namespace Orcamentaria.Lib.Application.Services
{
    public class RsaService : IRsaService
    {
        public RsaSecurityKey GenerateRsaSecurityKey(string projectName, string keyName)
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == projectName);

            var resourceName = assembly.GetManifestResourceNames()
                                       .FirstOrDefault(name => name.EndsWith(keyName));

            if (resourceName is null)
                throw new Exception($"Recurso {keyName} não encontrado.");

            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream);
            var textKey = reader.ReadToEnd();
            var rsa = RSA.Create();
            rsa.ImportFromPem(textKey.ToCharArray());
            return new RsaSecurityKey(rsa);
        }
    }
}
