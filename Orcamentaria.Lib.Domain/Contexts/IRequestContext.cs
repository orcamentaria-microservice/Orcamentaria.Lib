namespace Orcamentaria.Lib.Domain.Contexts
{
    public interface IRequestContext
    {
        public string RequestId { get; set; }
        int RequestOrderId { get; set; }
    }
}
