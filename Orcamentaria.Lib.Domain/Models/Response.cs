using FluentValidation.Results;

using Orcamentaria.Lib.Domain.Enums;
using System.Text.Json.Serialization;

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
                this.Error = new ResponseError(ResponseErrorEnum.NotFound);
                return;
            }

            this.Data = data;
        }

        public Response(T data, string simpleMessage)
        {
            this.Data = data;
            this.SimpleMessage = simpleMessage;
        }

        public Response(ResponseErrorEnum errorType)
        {
            this.Success = false;
            this.Error = new ResponseError(errorType);
        }

        public Response(ResponseErrorEnum errorType, string message)
        {
            this.Success = false;
            this.Error = new ResponseError(errorType, message);
        }
        public Response(ResponseErrorEnum errorType, string[] messages)
        {
            this.Success = false;
            this.Error = new ResponseError(errorType, messages);
        }

        public Response(ValidationResult result)
        {
            this.Success = false;
            this.Error = new ResponseError(
                ResponseErrorEnum.ValidationFailed, 
                result.Errors.Select(e => e.ErrorMessage).ToArray());
        }

        public Response(ResponseErrorEnum errorType, ValidationResult result)
        {
            this.Success = false;
            this.Error = new ResponseError(
                errorType,
                result.Errors.Select(e => e.ErrorMessage).ToArray());
        }
    }
}
