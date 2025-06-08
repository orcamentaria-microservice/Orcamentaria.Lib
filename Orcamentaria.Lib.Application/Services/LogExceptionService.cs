using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Orcamentaria.Lib.Domain.Exceptions;
using Orcamentaria.Lib.Domain.Models.Configurations;
using Orcamentaria.Lib.Domain.Models.Logs;
using Orcamentaria.Lib.Domain.Services;
using RabbitMQ.Client;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Orcamentaria.Lib.Application.Services
{
    public class LogExceptionService : ILogExceptionService
    {
        private readonly ServiceConfiguration _serviceConfiguration;

        public LogExceptionService(
            IOptions<ServiceConfiguration> serviceConfiguration)
        {
            _serviceConfiguration = serviceConfiguration.Value;
        }

        public async Task ResolveLog(DefaultException ex, HttpContext? context = null)
        {
            var stackTrace = new StackTrace(ex, true);
            var frame = stackTrace.GetFrame(0);

            RequestLog requestLog = null;

            if (context is not null)
            {
                context.Request.Headers.TryGetValue("RequestId", out StringValues requestId);
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

                requestLog = new RequestLog
                {
                    Id = requestId.FirstOrDefault(),
                    Host = request?.Host.ToString(),
                    Route = request?.Path,
                    Method = request?.Method,
                    Body = JsonSerializer.Serialize(body),
                    Query = JsonSerializer.Serialize(request?.Query)
                };
            }

            var exceptionLog = new ExceptionLog
            {
                Date = DateTime.Now,
                Message = ex.Message,
                Code = ex.ErrorCode  ?? 500,
                Type = ex.Type.ToString()!,
                Severity = ex.Severity.ToString()!,
                Request = requestLog,
                Place = new PlaceException
                {
                    ServiceName = _serviceConfiguration.ServiceName,
                    ProjectName = ex.Source!,
                    FunctionName = GetFunctionName(frame),
                    Line = frame.GetFileLineNumber()
                }
            };

            var message = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(exceptionLog));

            var factory = new ConnectionFactory { HostName = "localhost" };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: "exception_log", durable: false, exclusive: false, autoDelete: false,
                arguments: null);

            await channel.BasicPublishAsync(exchange: string.Empty, routingKey: "exception_log", body: message);
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
