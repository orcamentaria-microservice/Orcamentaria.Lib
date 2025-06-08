
using Orcamentaria.Lib.Domain.Enums;

namespace Orcamentaria.Lib.Domain.Exceptions
{
    public class DatabaseException : DefaultException
    {
        const SeverityLevelEnum defaultSeverityLevel = SeverityLevelEnum.Critical;
        const ErrorCodeEnum defaultErrorCode = ErrorCodeEnum.DatabaseError;

        public DatabaseException(
            string message,
            ErrorCodeEnum? errorCode = defaultErrorCode,
            SeverityLevelEnum? severity = defaultSeverityLevel) : 
            base(ExceptionTypeEnum.Database, severity, (int)errorCode, message)
        {
        }

        public DatabaseException(
            string message, 
            System.Exception exception,
            ErrorCodeEnum? errorCode = defaultErrorCode,
            SeverityLevelEnum? severity = defaultSeverityLevel) 
            : base(ExceptionTypeEnum.Database, severity, (int)errorCode, message, exception)
        {
        }
    }
}
