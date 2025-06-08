using Orcamentaria.Lib.Domain.Enums;

namespace Orcamentaria.Lib.Domain.Exceptions
{
    public abstract class DefaultException : System.Exception
    {
        public ExceptionTypeEnum? Type { get; } = ExceptionTypeEnum.Default;
        public SeverityLevelEnum? Severity { get; } = SeverityLevelEnum.Error;
        public int? ErrorCode { get; }

        protected DefaultException(
            ExceptionTypeEnum? type,
            SeverityLevelEnum? severity,
            int? errorCode,
            string message) : base(message)
        {
            Type = type;
            Severity = severity;
            ErrorCode = errorCode;
        }

        protected DefaultException(
            ExceptionTypeEnum? type,
            SeverityLevelEnum? severity,
            int? errorCode,
            string message,
            System.Exception exception) : base(message, exception)
        {
            Type = type;
            Severity = severity;
            ErrorCode = errorCode;
        }
    }
}
