using Orcamentaria.Lib.Domain.Exceptions;
using Orcamentaria.Lib.Domain.Models.Logs;

namespace Orcamentaria.Lib.Domain.Services
{
    public interface ILogService
    {
        Task ResolveLogAsync(DefaultException ex, ExceptionOrigin origin);
    }
}
