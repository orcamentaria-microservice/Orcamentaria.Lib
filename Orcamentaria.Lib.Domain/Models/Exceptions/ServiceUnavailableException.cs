
using Orcamentaria.Lib.Domain.Enums;

namespace Orcamentaria.Lib.Domain.Exceptions
{
    public class ServiceUnavailableException : DefaultException
    {
        const SeverityLevelEnum defaultSeverityLevel = SeverityLevelEnum.Critical;
        const ErrorCodeEnum defaultErrorCode = ErrorCodeEnum.ServiceUnavailable;

        public ServiceUnavailableException(
            string message,
            ErrorCodeEnum? errorCode = defaultErrorCode,
            SeverityLevelEnum? severity = defaultSeverityLevel) : 
            base(ExceptionTypeEnum.Integration, severity, (int)errorCode, message)
        {
        }

        public ServiceUnavailableException(
            string message, 
            System.Exception exception,
            ErrorCodeEnum? errorCode = defaultErrorCode,
            SeverityLevelEnum? severity = defaultSeverityLevel) : 
            base(ExceptionTypeEnum.Integration, severity, (int)errorCode, message, exception)
        {
        }
    }
}
