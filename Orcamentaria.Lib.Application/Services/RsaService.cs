using Microsoft.IdentityModel.Tokens;
using Orcamentaria.Lib.Domain.Exceptions;
using Orcamentaria.Lib.Domain.Services;
using System.Security.Cryptography;

namespace Orcamentaria.Lib.Application.Services
{
    public class RsaService : IRsaService
    {
        public RsaSecurityKey GenerateRsaSecurityKey(string projectName, string keyName)
        {
            try
            {
                if(String.IsNullOrEmpty(keyName))
                    throw new ConfigurationException($"Arquivo {keyName} inválido.");

                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == projectName);

                if (assembly is null)
                    throw new ConfigurationException($"Projeto {projectName} não encontrado.");

                var resourceName = assembly.GetManifestResourceNames()
                                           .FirstOrDefault(name => name.EndsWith(keyName));

                if (resourceName is null)
                    throw new ConfigurationException("Faltando arquivo de configuração.");

                using var stream = assembly.GetManifestResourceStream(resourceName);

                if (stream is null)
                    throw new ConfigurationException("Faltando arquivo de configuração.");

                using var reader = new StreamReader(stream);
                var textKey = reader.ReadToEnd();
                var rsa = RSA.Create();
                rsa.ImportFromPem(textKey.ToCharArray());
                return new RsaSecurityKey(rsa);
            }
            catch (DefaultException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new UnexpectedException(ex.Message, ex);
            }
        }
    }
}
