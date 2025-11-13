using Microsoft.AspNetCore.Http;
using Orcamentaria.Lib.Domain.Contexts;
using Orcamentaria.Lib.Domain.Exceptions;

namespace Orcamentaria.Lib.Infrastructure.Middlewares
{
    public class RequestMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IRequestContext requestContext)
        {
            try
            {
                if (!String.IsNullOrEmpty(context.Request.Headers["RequestId"]))
                {
                    requestContext.RequestId = context.Request.Headers["RequestId"];
                    requestContext.RequestOrderId = int.Parse(context.Request.Headers["RequestOrderId"]) + 1;
                }
                else
                    requestContext.RequestId = Guid.NewGuid().ToString();
            }
            catch (DefaultException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new UnexpectedException("Erro ao capturar dados do header/jwt token.", ex);
            }

            await _next(context);
        }
    }
}
