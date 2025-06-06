using Orcamentaria.Lib.Domain.Enums;
using Orcamentaria.Lib.Domain.Exceptions;

namespace Orcamentaria.Lib.Domain.Models.Exceptions
{
    public class InfoException : DefaultException
    {
        const SeverityLevelEnum defaultSeverityLevel = SeverityLevelEnum.Info;

        public InfoException(
            string message,
            ErrorCodeEnum errorCode,
            SeverityLevelEnum? severity = defaultSeverityLevel) :
            base(ExceptionTypeEnum.Info, severity, errorCode, message)
        {
        }

        public InfoException(
            string message,
            System.Exception exception,
            ErrorCodeEnum errorCode,
            SeverityLevelEnum? severity = defaultSeverityLevel)
            : base(ExceptionTypeEnum.Info, severity, errorCode, message, exception)
        {
        }
    }
}
