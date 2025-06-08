using Microsoft.AspNetCore.Http;
using Orcamentaria.Lib.Domain.Enums;
using Orcamentaria.Lib.Domain.Exceptions;
using Orcamentaria.Lib.Domain.Models.Logs;

namespace Orcamentaria.Lib.Domain.Services
{
    public interface ILogExceptionService
    {
        Task ResolveLog(DefaultException ex, ExceptionOrigin origin);
    }
}
