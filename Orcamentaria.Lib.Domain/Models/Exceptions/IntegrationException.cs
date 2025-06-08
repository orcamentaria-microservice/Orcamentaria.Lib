
using Orcamentaria.Lib.Domain.Enums;
using System.Net;

namespace Orcamentaria.Lib.Domain.Exceptions
{
    public class IntegrationException : DefaultException
    {
        const SeverityLevelEnum defaultSeverityLevel = SeverityLevelEnum.Critical;

        public IntegrationException(
            string message,
            HttpStatusCode errorCode,
            SeverityLevelEnum? severity = defaultSeverityLevel) : 
            base(ExceptionTypeEnum.Integration, severity, (int)errorCode, message)
        {
        }

        public IntegrationException(
            string message, 
            System.Exception exception,
            HttpStatusCode errorCode,
            SeverityLevelEnum? severity = defaultSeverityLevel) 
            : base(ExceptionTypeEnum.Integration, severity, (int)errorCode, message, exception)
        {
        }
    }
}
