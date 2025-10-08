using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orcamentaria.Lib.Domain.Contexts;
using Orcamentaria.Lib.Domain.Exceptions;
using Orcamentaria.Lib.Domain.Models.Configurations;
using Orcamentaria.Lib.Domain.Models.Logs;
using Orcamentaria.Lib.Domain.Services;
using System.Diagnostics;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Orcamentaria.Lib.Application.Services
{
    public class LogService : ILogService
    {
        private readonly ServiceConfiguration _serviceConfiguration;
        private readonly IPublishMessageBrokerService _publishMessageBrokerService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LogService(
            IOptions<ServiceConfiguration> serviceConfiguration,
            IPublishMessageBrokerService publishMessageBrokerService,
            IHttpContextAccessor httpContextAccessor)
        {
            _serviceConfiguration = serviceConfiguration.Value;
            _publishMessageBrokerService = publishMessageBrokerService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task ResolveLogAsync(DefaultException ex, ExceptionOrigin origin)
        {
            var requestContext = GetContext();
            var stackTrace = new StackTrace(ex, true);
            var frame = stackTrace.GetFrame(0);

            var exceptionLog = new ExceptionLog
            {
                TraceId = requestContext is null ? Guid.NewGuid().ToString() : requestContext.RequestId,
                TraceOrderId = requestContext is null ? "0" : requestContext.RequestOrderId.ToString(),
                Date = DateTime.Now,
                Message = ex.Message,
                Code = ex.ErrorCode ?? 500,
                Type = ex.Type.ToString()!,
                Severity = ex.Severity.ToString()!,
                Origin = origin,
                Place = new PlaceException
                {
                    ServiceName = _serviceConfiguration.ServiceName,
                    ProjectName = ex.Source!,
                    FunctionName = GetFunctionName(frame),
                    Line = frame.GetFileLineNumber()
                }
            };

            var routingKey = $"error.{ex.Severity.ToString().ToLower()}";
;
            await _publishMessageBrokerService.SendMessageToTopicExchange(
                message: JsonSerializer.Serialize(exceptionLog), 
                exchange: "error", 
                routingKey: routingKey,
                binds: ["*", "critical"]);
        }

        private string GetFunctionName(StackFrame frame)
        {
            var method = frame.GetMethod();
            if (method?.Name != "MoveNext" && method?.DeclaringType?.Name != "ThrowHelper")
                return method.Name;

            if (method?.Name == "MoveNext" && method.DeclaringType?.Name?.Contains("<") == true)
            {
                var originalTypeName = method.DeclaringType.Name;
                var methodName = originalTypeName.Substring(originalTypeName.IndexOf("<<") + 2);
                methodName = methodName.Substring(0, methodName.IndexOf(">"));
                return methodName;
            }

            return method.Name;
        }

        private IRequestContext GetContext()
        {
            return _httpContextAccessor.HttpContext?.RequestServices.GetService<IRequestContext>();
        }
    }
}
