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
            this.Success = true;
        }
        
        public Response(T data)
        {
            if(data is null)
            {
                this.Success = false;
                this.Error = new ResponseError(ErrorCodeEnum.NotFound);
                return;
            }

            this.Data = data;
        }

        public Response(T data, string simpleMessage)
        {
            this.Data = data;
            this.SimpleMessage = simpleMessage;
        }

        public Response(ErrorCodeEnum errorType)
        {
            this.Success = false;
            this.Error = new ResponseError(errorType);
        }

        public Response(ErrorCodeEnum errorType, string message)
        {
            this.Success = false;
            this.Error = new ResponseError(errorType, message);
        }

        public Response(ErrorCodeEnum errorType, string[] messages)
        {
            this.Success = false;
            this.Error = new ResponseError(errorType, messages);
        }

        public Response(ValidationResult result)
        {
            this.Success = false;
            this.Error = new ResponseError(
                ErrorCodeEnum.ValidationFailed, 
                result.Errors.Select(e => e.ErrorMessage).ToArray());
        }

        public Response(ErrorCodeEnum errorType, ValidationResult result)
        {
            this.Success = false;
            this.Error = new ResponseError(
                errorType,
                result.Errors.Select(e => e.ErrorMessage).ToArray());
        }
    }
}
