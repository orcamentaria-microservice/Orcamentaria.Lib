using Polly;
using Polly.CircuitBreaker;
using Polly.Fallback;

namespace Orcamentaria.Lib.Domain.Services
{
    public interface IResilienceService<T> where T : class
    {
        PredicateBuilder<T> CreatePredicate();
        Func<FallbackActionArguments<T>, ValueTask<Outcome<T>>> FallbackAction();
    }

    public interface IResilienceHandleService<T> where T : class
    {
        ResiliencePipeline<T> Pipeline { get; }
        CircuitBreakerStateProvider? CircuitState { get; }
    }
}
