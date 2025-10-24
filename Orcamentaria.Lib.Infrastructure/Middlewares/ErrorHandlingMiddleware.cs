using Microsoft.AspNetCore.Http;
using Orcamentaria.Lib.Domain.Enums;
using Orcamentaria.Lib.Domain.Exceptions;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Models.Logs;
using Orcamentaria.Lib.Domain.Services;
using System.Text;
using System.Text.Json;

namespace Orcamentaria.Lib.Infrastructure.Middlewares
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogService _logExceptionService;

        public ErrorHandlingMiddleware(
            RequestDelegate next, 
            ILogService logExceptionService)
        {
            _next = next;
            _logExceptionService = logExceptionService;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (DefaultException ex)
            {
               await HandleExceptionAsync(httpContext, ex);
            }
        }

        public async Task HandleExceptionAsync(HttpContext context, DefaultException ex)
        {
            var request = context.Request;

            string body = "";
            request.EnableBuffering();
            request.Body.Position = 0;

            using (var reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024, true))
            {
                body = await reader.ReadToEndAsync();

                if (String.IsNullOrEmpty(body))
                    body = "{}";
            }

            var origin = new RequestExceptionOrigin
            {
                Type = OriginEnum.External,
                Host = request?.Host.ToString(),
                Route = request?.Path,
                Method = request?.Method,
                Body = JsonSerializer.Serialize(body),
                Query = JsonSerializer.Serialize(request?.Query)
            };

            await _logExceptionService.ResolveLogAsync(ex, origin);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)ex.ErrorCode;

            var messages = ex.Message.Split(" || ");

            if(messages.Length > 0)
            {
                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(new Response<dynamic>(
                        (ErrorCodeEnum)ex.ErrorCode, messages)));
                return;
            }

            await context.Response.WriteAsync(
                    JsonSerializer.Serialize(new Response<dynamic>(
                        (ErrorCodeEnum)ex.ErrorCode, ex.Message)));
        }
    }
}
