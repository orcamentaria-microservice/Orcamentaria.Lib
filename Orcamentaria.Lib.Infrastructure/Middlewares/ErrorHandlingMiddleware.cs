using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Orcamentaria.Lib.Domain.Enums;
using Orcamentaria.Lib.Domain.Exceptions;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Services;
using System.Net;
using System.Text.Json;

namespace Orcamentaria.Lib.Infrastructure.Middlewares
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogExceptionService _logExceptionService;

        public ErrorHandlingMiddleware(
            RequestDelegate next, 
            ILogExceptionService logExceptionService)
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
            await _logExceptionService.ResolveLog(ex, context);

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
