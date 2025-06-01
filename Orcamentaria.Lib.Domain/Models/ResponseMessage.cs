namespace Orcamentaria.Lib.Domain.Models
{
    public class ResponseMessage
    {
        public int Index { get; set; }
        public string Message { get; set; }

        public ResponseMessage() { }

        public ResponseMessage(int index, string message)
        {
            this.Index = index;
            this.Message = message;
        }

        public ResponseMessage(string message)
        {
            Index = 0;
            Message = message;
        }
    }
}
