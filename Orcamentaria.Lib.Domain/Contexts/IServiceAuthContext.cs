namespace Orcamentaria.Lib.Domain.Contexts
{
    public interface IServiceAuthContext
    {
        public long ServiceId { get; set; }
        public string ServiceName { get; set; }
        public string ServiceToken { get; set; }
    }
}
