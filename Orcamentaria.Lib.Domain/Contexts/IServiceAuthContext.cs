namespace Orcamentaria.Lib.Domain.Contexts
{
    public interface IServiceAuthContext
    {
        public long ServiceId { get; set; }
        public string Name { get; set; }
        public string BearerToken { get; set; }
    }
}
