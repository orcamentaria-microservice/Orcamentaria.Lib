using Orcamentaria.Lib.Domain.Enums;

namespace Orcamentaria.Lib.Domain.Models.Responses
{
    public class ResponseError
    {
        public ErrorCodeEnum ErrorCode { get; set; }
        public string ErrorName { get; set; }
        public IEnumerable<ResponseMessage> MessageErrors { get; set; }

        public ResponseError() { }

        public ResponseError(ErrorCodeEnum errorType)
        {
            ErrorCode = errorType;
            ErrorName = errorType.ToString();
        }

        public ResponseError(ErrorCodeEnum errorType, IEnumerable<ResponseMessage> messageErrors)
        {
            ErrorCode = errorType;
            ErrorName = errorType.ToString();
            MessageErrors = messageErrors;
        }

        public ResponseError(ErrorCodeEnum errorType, string message)
        {
            ErrorCode = errorType;
            ErrorName = errorType.ToString();
            MessageErrors = new List<ResponseMessage>() { new ResponseMessage(message) };
        }

        public ResponseError(ErrorCodeEnum errorType, string[] messages)
        {
            ErrorCode = errorType;
            ErrorName = errorType.ToString();
            MessageErrors = messages.Select((message, index) => new ResponseMessage(index, message));
        }
    }
}
