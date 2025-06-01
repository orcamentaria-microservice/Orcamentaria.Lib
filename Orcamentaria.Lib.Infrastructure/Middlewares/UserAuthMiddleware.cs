using Microsoft.AspNetCore.Http;
using Orcamentaria.Lib.Domain.Contexts;
using System.IdentityModel.Tokens.Jwt;

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
            long.TryParse(context.Request.HttpContext.User.Claims
                .Where(c => c.Type == JwtRegisteredClaimNames.Sub).First().Value, out var userId);
            var email = context.Request.HttpContext.User.Claims
                .Where(c => c.Type == JwtRegisteredClaimNames.Email).First().Value;
            long.TryParse(context.Request.HttpContext.User.Claims
                .Where(c => c.Type == "Company").First().Value, out var companyId);

            userAuthContext.UserId = userId;
            userAuthContext.UserEmail = email;
            userAuthContext.UserCompanyId = companyId;

            await _next(context);
        }
    }
}
