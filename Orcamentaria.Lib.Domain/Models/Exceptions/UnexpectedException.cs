
using Orcamentaria.Lib.Domain.Enums;

namespace Orcamentaria.Lib.Domain.Exceptions
{
    public class UnexpectedException : DefaultException
    {
        const SeverityLevelEnum defaultSeverityLevel = SeverityLevelEnum.Error;
        const ErrorCodeEnum defaultErrorCode = ErrorCodeEnum.InternalError;

        public UnexpectedException(
            string message,
            ErrorCodeEnum? errorCode = defaultErrorCode,
            SeverityLevelEnum? severity = defaultSeverityLevel) : 
            base(ExceptionTypeEnum.Unexpected, severity, (int)errorCode, message)
        {
        }

        public UnexpectedException(
            string message, 
            System.Exception exception,
            ErrorCodeEnum? errorCode = defaultErrorCode,
            SeverityLevelEnum? severity = defaultSeverityLevel) : 
            base(ExceptionTypeEnum.Unexpected, severity, (int)errorCode, message, exception)
        {
        }
    }
}
