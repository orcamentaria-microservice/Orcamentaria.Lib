using Orcamentaria.Lib.Domain.Enums;
using Orcamentaria.Lib.Domain.Exceptions;

namespace Orcamentaria.Lib.Domain.Models.Exceptions
{
    public class BusinessException : DefaultException
    {
        const SeverityLevelEnum defaultSeverityLevel = SeverityLevelEnum.Warning;

        public BusinessException(
            string message,
            ErrorCodeEnum errorCode,
            SeverityLevelEnum? severity = defaultSeverityLevel) :
            base(ExceptionTypeEnum.Business, severity, (int)errorCode, message)
        {
        }

        public BusinessException(
            string message,
            System.Exception exception,
            ErrorCodeEnum errorCode,
            SeverityLevelEnum? severity = defaultSeverityLevel)
            : base(ExceptionTypeEnum.Business, severity, (int)errorCode, message, exception)
        {
        }
    }
}
