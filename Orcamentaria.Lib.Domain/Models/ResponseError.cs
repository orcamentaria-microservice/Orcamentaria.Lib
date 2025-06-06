using Orcamentaria.Lib.Domain.Enums;

namespace Orcamentaria.Lib.Domain.Models
{
    public class ResponseError
    {
        public ErrorCodeEnum ErrorCode { get; set; }
        public string ErrorName { get; set; }
        public IEnumerable<ResponseMessage> MessageErrors { get; set; }

        public ResponseError() { }

        public ResponseError(ErrorCodeEnum errorType) 
        {
            this.ErrorCode = errorType;
            this.ErrorName = errorType.ToString();
        }

        public ResponseError(ErrorCodeEnum errorType, IEnumerable<ResponseMessage> messageErrors)
        {
            this.ErrorCode = errorType;
            this.ErrorName = errorType.ToString();
            this.MessageErrors = messageErrors;
        }

        public ResponseError(ErrorCodeEnum errorType, string message)
        {
            this.ErrorCode = errorType;
            this.ErrorName = errorType.ToString();
            this.MessageErrors = new List<ResponseMessage>() { new ResponseMessage(message) };
        }

        public ResponseError(ErrorCodeEnum errorType, string[] messages)
        {
            this.ErrorCode = errorType;
            this.ErrorName = errorType.ToString();
            this.MessageErrors = messages.Select((message, index) => new ResponseMessage(index, message));
        }
    }
}
