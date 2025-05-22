

using Microsoft.AspNetCore.Http;
using Orcamentaria.Lib.Domain.Contexts;

namespace Orcamentaria.Lib.Infrastructure.Middlewares
{
    public class CompanyMiddleware
    {
        private readonly RequestDelegate _next;

        public CompanyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ICompanyContext companyContext)
        {
            if (context.Request.Headers.TryGetValue("CompanyId", out var companyIdHeader) &&
                int.TryParse(companyIdHeader, out var companyId))
            {
                companyContext.CompanyId = companyId;
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("CompanyId header is missing or invalid.");
                return;
            }

            await _next(context);
        }
    }
}
