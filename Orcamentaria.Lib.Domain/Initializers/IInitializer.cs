using Microsoft.Extensions.Configuration;
using Orcamentaria.Lib.Domain.Models.Configurations;

namespace Orcamentaria.Lib.Domain.Initializers
{
    public interface IInitializer
    {
        Task InitializeAsync(IConfiguration configuration);
    }
}
