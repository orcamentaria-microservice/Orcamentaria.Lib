using Microsoft.AspNetCore.Http;
using Orcamentaria.Lib.Domain.Contexts;

namespace Orcamentaria.Lib.Infrastructure.Middlewares
{
    public class UserAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public UserAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IUserAuthContext userAuthContext)
        {
            var claims = context.Request.HttpContext.User.Claims;

            if (claims.Any())
            {
                long.TryParse(claims.Where(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").First().Value, out var userId);
                var email = claims.Where(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress").First().Value;
                long.TryParse(claims.Where(c => c.Type == "Company").First().Value, out var companyId);
                var token = context.Request.Headers.Authorization.First()?.Replace("Bearer ", "")!;

                userAuthContext.UserId = userId;
                userAuthContext.UserEmail = email;
                userAuthContext.UserCompanyId = companyId;
                userAuthContext.UserToken = token;
            }

            await _next(context);
        }
    }
}
