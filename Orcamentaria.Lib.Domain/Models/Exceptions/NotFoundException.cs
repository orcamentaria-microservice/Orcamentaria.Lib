using Orcamentaria.Lib.Domain.Enums;
using Orcamentaria.Lib.Domain.Exceptions;
using System.Net;

namespace Orcamentaria.Lib.Domain.Models.Exceptions
{
    public class NotFoundException : DefaultException
    {
        const SeverityLevelEnum defaultSeverityLevel = SeverityLevelEnum.Warning;
        const ErrorCodeEnum defaultErrorCode = ErrorCodeEnum.NotFound;

        public NotFoundException(
            string message,
            HttpStatusCode? errorCode = (HttpStatusCode)defaultErrorCode,
            SeverityLevelEnum? severity = defaultSeverityLevel) :
            base(ExceptionTypeEnum.NotFound, severity, (int)errorCode, message)
        {
        }

        public NotFoundException(
            string message,
            System.Exception exception,
            HttpStatusCode? errorCode = (HttpStatusCode)defaultErrorCode,
            SeverityLevelEnum? severity = defaultSeverityLevel)
            : base(ExceptionTypeEnum.NotFound, severity, (int)errorCode, message, exception)
        {
        }
    }
}
