
using Orcamentaria.Lib.Domain.Enums;

namespace Orcamentaria.Lib.Domain.Exceptions
{
    public class ConfigurationException : DefaultException
    {
        const SeverityLevelEnum defaultSeverityLevel = SeverityLevelEnum.Error;
        const ErrorCodeEnum defaultErrorCode = ErrorCodeEnum.InternalError;

        public ConfigurationException(
            string message,
            ErrorCodeEnum? errorCode = defaultErrorCode,
            SeverityLevelEnum? severity = defaultSeverityLevel) : 
            base(ExceptionTypeEnum.Configuration, severity, errorCode, message)
        {
        }

        public ConfigurationException(
            string message, 
            System.Exception exception,
            ErrorCodeEnum? errorCode = defaultErrorCode,
            SeverityLevelEnum? severity = defaultSeverityLevel) : 
            base(ExceptionTypeEnum.Configuration, severity, errorCode, message, exception)
        {
        }
    }
}
