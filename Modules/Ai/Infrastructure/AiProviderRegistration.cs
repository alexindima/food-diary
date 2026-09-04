using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Integrations.Options;
using FoodDiary.Integrations.Services.OpenAi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace FoodDiary.Infrastructure;

public static class AiProviderRegistration {
    public static IServiceCollection AddAiProvider(this IServiceCollection services, IConfiguration configuration) {
        services.AddOptions<OpenAiOptions>()
            .Bind(configuration.GetSection(OpenAiOptions.SectionName))
            .Validate(OpenAiOptions.HasVisionFallbackWhenVisionModelConfigured,
                "OpenAi:VisionFallbackModel is required when VisionModel is configured.")
            .Validate(OpenAiOptions.HasTextModelWhenApiKeyConfigured,
                "OpenAi:TextModel is required when ApiKey is configured.")
            .Validate(OpenAiOptions.HasVisionModelWhenApiKeyConfigured,
                "OpenAi:VisionModel is required when ApiKey is configured.")
            .Validate(OpenAiOptions.HasValidMaxOutputTokens,
                "OpenAi:MaxOutputTokens must be between 1 and 32768.")
            .ValidateOnStart();
        services.AddHttpClient<IOpenAiFoodClient, OpenAiFoodClient>(client => client.Timeout = TimeSpan.FromSeconds(60))
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
