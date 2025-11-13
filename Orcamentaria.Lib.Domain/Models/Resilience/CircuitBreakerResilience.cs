using Polly.CircuitBreaker;

namespace Orcamentaria.Lib.Domain.Models.Resilience
{
    public class CircuitBreakerResilience<T>
    {
        public string? Name { get; set; }
        public double FailureRatio { get; set; } = 0.5;
        public int MinimumThroughput { get; set; } = 5;
        public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(60);
        public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(30);
        public Func<OnCircuitClosedArguments<T>, ValueTask>? OnClosed { get; set; } = default;
        public Func<OnCircuitHalfOpenedArguments, ValueTask>? OnHalfOpened { get; set; } = default;
        public Func<OnCircuitOpenedArguments<T>, ValueTask>? OnOpened { get; set; } = default;
    }
}
