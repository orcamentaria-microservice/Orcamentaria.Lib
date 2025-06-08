using Microsoft.AspNetCore.Http;
using Orcamentaria.Lib.Domain.Exceptions;

namespace Orcamentaria.Lib.Domain.Services
{
    public interface ILogExceptionService
    {
        Task ResolveLog(DefaultException ex, HttpContext? context = null);
    }
}
