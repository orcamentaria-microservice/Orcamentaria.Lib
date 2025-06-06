
using Orcamentaria.Lib.Domain.Enums;

namespace Orcamentaria.Lib.Domain.Exceptions
{
    public class IntegrationException : DefaultException
    {
        const SeverityLevelEnum defaultSeverityLevel = SeverityLevelEnum.Critical;
        const ErrorCodeEnum defaultErrorCode = ErrorCodeEnum.ExternalServiceFailure;

        public IntegrationException(
            string message,
            ErrorCodeEnum? errorCode = defaultErrorCode,
            SeverityLevelEnum? severity = defaultSeverityLevel) : 
            base(ExceptionTypeEnum.Integration, severity, errorCode, message)
        {
        }

        public IntegrationException(
            string message, 
            System.Exception exception,
            ErrorCodeEnum? errorCode = defaultErrorCode,
            SeverityLevelEnum? severity = defaultSeverityLevel) 
            : base(ExceptionTypeEnum.Integration, severity, errorCode, message, exception)
        {
        }
    }
}
