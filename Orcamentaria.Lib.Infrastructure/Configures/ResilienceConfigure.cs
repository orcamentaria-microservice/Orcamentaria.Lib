using Microsoft.Extensions.DependencyInjection;
using Orcamentaria.Lib.Domain.Models.Resilience;
using Orcamentaria.Lib.Domain.Services;
using Polly;
using Polly.CircuitBreaker;

namespace Orcamentaria.Lib.Infrastructure.Configures
{
    public static class ResilienceConfigure
    {
        public static IServiceCollection ConfigureResilience<T, R>(
            this IServiceCollection services,
            RetryResilience? retryResilience = null,
            CircuitBreakerResilience<T>? circuitBreakerResilience = null)
            where T : class
            where R : class, IResilienceService<T>
        {
            services.AddScoped<IResilienceService<T>, R>();

            services.AddSingleton<IResilienceHandleService<T>>(sp =>
            {
                using var scope = sp.CreateScope();
                var resilience = scope.ServiceProvider.GetRequiredService<IResilienceService<T>>();
                var predicate = resilience.CreatePredicate();

                var builder = new ResiliencePipelineBuilder<T>()
                    .AddFallback(new()
                    {
                        ShouldHandle = predicate,
                        FallbackAction = resilience.FallbackAction()
                    });

                CircuitBreakerStateProvider? stateProvider = null;

                if (retryResilience is not null)
                {
                    builder.AddRetry(new()
                    {
                        ShouldHandle = predicate,
                        MaxRetryAttempts = retryResilience.MaxRetryAttempts,
                        Delay = retryResilience.Delay
                    });
                }

                if (circuitBreakerResilience is not null)
                {
                    stateProvider = new CircuitBreakerStateProvider();

                    builder.AddCircuitBreaker(new()
                    {
                        ShouldHandle = predicate,
                        FailureRatio = circuitBreakerResilience.FailureRatio,
                        MinimumThroughput = circuitBreakerResilience.MinimumThroughput,
                        SamplingDuration = circuitBreakerResilience.SamplingDuration,
                        BreakDuration = circuitBreakerResilience.BreakDuration,
                        OnClosed = circuitBreakerResilience.OnClosed,
                        OnHalfOpened = circuitBreakerResilience.OnHalfOpened,
                        OnOpened = circuitBreakerResilience.OnOpened,
                        StateProvider = stateProvider,
                        Name = circuitBreakerResilience.Name ?? typeof(T).Name
                    });
                }

                var pipeline = builder.Build();

                return new ResilienceHandle<T>(pipeline, stateProvider);
            });

            services.AddSingleton<ResiliencePipeline<T>>(sp =>
                sp.GetRequiredService<IResilienceHandleService<T>>().Pipeline);

            services.AddSingleton<CircuitBreakerStateProvider?>(sp =>
                sp.GetRequiredService<IResilienceHandleService<T>>().CircuitState);

            return services;
        }

        private sealed record ResilienceHandle<T>(
            ResiliencePipeline<T> Pipeline,
            CircuitBreakerStateProvider? CircuitState
        ) : IResilienceHandleService<T> where T : class;
    }
}
