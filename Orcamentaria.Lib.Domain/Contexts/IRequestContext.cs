namespace Orcamentaria.Lib.Domain.Contexts
{
    public interface IRequestContext
    {
        public string RequestId { get; set; }
        public int RequestOrderId { get; set; }
    }
}
