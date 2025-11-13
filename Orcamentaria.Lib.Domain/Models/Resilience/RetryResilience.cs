namespace Orcamentaria.Lib.Domain.Models.Resilience
{
    public class RetryResilience
    {
        public int MaxRetryAttempts { get; set; } = 3;
        public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(1);
    }
}
