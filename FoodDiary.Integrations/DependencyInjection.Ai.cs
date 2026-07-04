using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Integrations.Services.OpenAi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace FoodDiary.Integrations;

public static partial class DependencyInjection {
    private static IServiceCollection AddAiIntegrations(this IServiceCollection services) {
        services.AddHttpClient<IOpenAiFoodClient, OpenAiFoodClient>(client => {
            client.Timeout = TimeSpan.FromSeconds(60);
        })
        .AddResilienceHandler("openai-circuit-breaker", builder => {
            builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 3,
                BreakDuration = TimeSpan.FromSeconds(30),
            });
        });

        return services;
    }
}
