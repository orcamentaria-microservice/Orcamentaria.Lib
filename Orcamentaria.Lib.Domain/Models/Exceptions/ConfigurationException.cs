
using Orcamentaria.Lib.Domain.Enums;

namespace Orcamentaria.Lib.Domain.Exceptions
{
    public class ConfigurationException : DefaultException
    {
        const SeverityLevelEnum defaultSeverityLevel = SeverityLevelEnum.Critical;
        const ErrorCodeEnum defaultErrorCode = ErrorCodeEnum.InternalError;

        public ConfigurationException(
            string message,
            ErrorCodeEnum? errorCode = defaultErrorCode,
            SeverityLevelEnum? severity = defaultSeverityLevel) : 
            base(ExceptionTypeEnum.Configuration, severity, (int)errorCode, message)
        {
        }

        public ConfigurationException(
            string message, 
            System.Exception exception,
            ErrorCodeEnum? errorCode = defaultErrorCode,
            SeverityLevelEnum? severity = defaultSeverityLevel) : 
            base(ExceptionTypeEnum.Configuration, severity, (int)errorCode, message, exception)
        {
        }
    }
}
