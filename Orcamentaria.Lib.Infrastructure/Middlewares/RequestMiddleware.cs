using Microsoft.AspNetCore.Http;
using Orcamentaria.Lib.Domain.Contexts;

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
                if(!String.IsNullOrEmpty(context.Request.Headers["RequestId"]))
                {
                    requestContext.RequestId = context.Request.Headers["RequestId"];
                    requestContext.RequestOrderId = int.Parse(context.Request.Headers["RequestOrderId"]);
                }

                await _next(context);
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
