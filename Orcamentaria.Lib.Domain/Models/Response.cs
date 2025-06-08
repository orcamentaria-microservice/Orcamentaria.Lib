using FluentValidation.Results;
using Orcamentaria.Lib.Domain.Enums;

namespace Orcamentaria.Lib.Domain.Models
{
    public class Response<T>
    {
        public T? Data { get; set; }
        public bool Success { get; set; } = true;
        public string? SimpleMessage { get; set; }
        public ResponseError? Error { get; set; }

        public Response()
        {
            Success = true;
        }

        public Response(T data)
        {
            if (data is null)
            {
                Success = false;
                Error = new ResponseError(ErrorCodeEnum.NotFound);
                return;
            }

            Data = data;
        }

        public Response(T data, string simpleMessage)
        {
            Data = data;
            SimpleMessage = simpleMessage;
        }

        public Response(ErrorCodeEnum errorType)
        {
            Success = false;
            Error = new ResponseError(errorType);
        }

        public Response(ErrorCodeEnum errorType, string message)
        {
            Success = false;
            Error = new ResponseError(errorType, message);
        }

        public Response(ErrorCodeEnum errorType, string[] messages)
        {
            Success = false;
            Error = new ResponseError(errorType, messages);
        }

        public Response(ValidationResult result)
        {
            Success = false;
            Error = new ResponseError(
                ErrorCodeEnum.ValidationFailed,
                result.Errors.Select(e => e.ErrorMessage).ToArray());
        }

        public Response(ErrorCodeEnum errorType, ValidationResult result)
        {
            Success = false;
            Error = new ResponseError(
                errorType,
                result.Errors.Select(e => e.ErrorMessage).ToArray());
        }
    }
}
