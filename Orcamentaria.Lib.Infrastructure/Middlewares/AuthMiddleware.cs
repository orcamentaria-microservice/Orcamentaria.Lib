using Microsoft.AspNetCore.Http;
using Orcamentaria.Lib.Domain.Contexts;
using System.IdentityModel.Tokens.Jwt;

namespace Orcamentaria.Lib.Infrastructure.Middlewares
{
    public class AuthMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context, 
            IUserAuthContext userAuthContext, 
            IServiceAuthContext serviceAuthContext)
        {
            try
            {
                var principal = context.User;

                if (principal?.Identity?.IsAuthenticated == false)
                {
                    await _next(context);
                    return;
                }
                
                var header = context.Request.Headers.Authorization.FirstOrDefault();
                userAuthContext.UserToken = TryGetBearer(header);

                var tokenUse = principal.FindFirst("token_use")?.Value;
                var audiences = principal.Claims
                    .Where(c => c.Type == "aud" || c.Type == JwtRegisteredClaimNames.Aud)
                    .Select(c => c.Value)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if(string.Equals(tokenUse, "bootstrap", StringComparison.OrdinalIgnoreCase))
                {
                    await _next(context);
                    return;
                }
                    
                var claims = principal.Claims;

                if (string.Equals(tokenUse, "user", StringComparison.OrdinalIgnoreCase))
                {
                    long.TryParse(claims.Where(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").First().Value, out var userId);
                    var email = claims.Where(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress").First().Value;
                    long.TryParse(claims.Where(c => c.Type == "Company").First().Value, out var companyId);

                    userAuthContext.UserId = userId;
                    userAuthContext.UserEmail = email;
                    userAuthContext.UserCompanyId = companyId;
                        
                    await _next(context);
                    return;
                }

                if (string.Equals(tokenUse, "service", StringComparison.OrdinalIgnoreCase))
                {
                    long.TryParse(claims.Where(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").First().Value, out var serviceId);
                    var serviceName = claims.Where(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name").First().Value;

                    serviceAuthContext.ServiceId = serviceId;
                    userAuthContext.UserEmail = serviceName;

                    await _next(context);
                    return;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        #region private methods
        private static string? TryGetBearer(string? authHeader)
        {
            if (string.IsNullOrWhiteSpace(authHeader)) return null;
            const string prefix = "Bearer ";
            return authHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? authHeader.Substring(prefix.Length).Trim()
                : null;
        }
        #endregion
    }
}
