using Microsoft.Extensions.Options;
using Orcamentaria.Lib.Domain.Exceptions;
using Orcamentaria.Lib.Domain.Models.Configurations;
using Orcamentaria.Lib.Domain.Models.Logs;
using Orcamentaria.Lib.Domain.Services;
using System.Diagnostics;
using System.Text.Json;

namespace Orcamentaria.Lib.Application.Services
{
    public class LogService : ILogService
    {
        private readonly ServiceConfiguration _serviceConfiguration;
        private readonly IPublishMessageBrokerService _publishMessageBrokerService;

        public LogService(
            IOptions<ServiceConfiguration> serviceConfiguration,
            IPublishMessageBrokerService publishMessageBrokerService)
        {
            _serviceConfiguration = serviceConfiguration.Value;
            _publishMessageBrokerService = publishMessageBrokerService;
        }

        public async Task ResolveLogAsync(DefaultException ex, ExceptionOrigin origin)
        {
            var stackTrace = new StackTrace(ex, true);
            var frame = stackTrace.GetFrame(0);

            var exceptionLog = new ExceptionLog
            {
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
    }
}
