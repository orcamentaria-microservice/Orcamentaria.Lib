using FluentValidation.Results;
using Orcamentaria.Lib.Domain.Enums;

namespace Orcamentaria.Lib.Domain.Exceptions
{
    public class ValidationException : DefaultException
    {
        const SeverityLevelEnum defaultSeverityLevel = SeverityLevelEnum.Warning;
        const ErrorCodeEnum defaultErrorCode = ErrorCodeEnum.ValidationFailed;

        public ValidationException(
            string message,
            ErrorCodeEnum? errorCode = defaultErrorCode,
            SeverityLevelEnum? severity = defaultSeverityLevel) : 
            base(ExceptionTypeEnum.Validation, severity, errorCode, message)
        {
        }

        public ValidationException(
            string message, 
            System.Exception exception,
            ErrorCodeEnum? errorCode = defaultErrorCode,
            SeverityLevelEnum? severity = defaultSeverityLevel) : 
            base(ExceptionTypeEnum.Validation, severity!, errorCode, message, exception)
        {
        }

        public ValidationException(
            ValidationResult validation) :
            base(
                ExceptionTypeEnum.Validation, 
                defaultSeverityLevel,
                defaultErrorCode,
                String.Join(" || ", validation.Errors.Select(e => e.ErrorMessage)))
        {
        }
    }
}
