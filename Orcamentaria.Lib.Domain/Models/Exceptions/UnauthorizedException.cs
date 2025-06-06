
using Orcamentaria.Lib.Domain.Enums;

namespace Orcamentaria.Lib.Domain.Exceptions
{
    public class UnauthorizedException : DefaultException
    {
        const SeverityLevelEnum defaultSeverityLevel = SeverityLevelEnum.Warning;
        const ErrorCodeEnum defaultErrorCode = ErrorCodeEnum.AccessDenied;

        public UnauthorizedException(
            string message,
            ErrorCodeEnum? errorCode = defaultErrorCode,
            SeverityLevelEnum? severity = defaultSeverityLevel) : 
            base(ExceptionTypeEnum.Unauthorized, severity, errorCode, message)
        {
        }

        public UnauthorizedException(
            string message, 
            System.Exception exception,
            ErrorCodeEnum? errorCode = defaultErrorCode,
            SeverityLevelEnum? severity = defaultSeverityLevel) : 
            base(ExceptionTypeEnum.Unauthorized, severity, errorCode, message, exception)
        {
        }
    }
}
