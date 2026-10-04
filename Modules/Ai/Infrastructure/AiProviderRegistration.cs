using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Options;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Services.OpenAi;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Services.Recipes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace FoodDiary.Modules.Ai.Infrastructure;

public static class AiProviderRegistration {
    public static IServiceCollection AddAiProvider(this IServiceCollection services, IConfiguration configuration) {
        services.AddOptions<RecipeVideoOptions>().Bind(configuration.GetSection(RecipeVideoOptions.SectionName))
            .Validate(RecipeVideoOptions.IsValid, "RecipeVideo:FfmpegPath must not be empty.").ValidateOnStart();
        services.AddHttpClient<IRecipeVideoProcessor, RecipeVideoProcessor>(client => client.Timeout = TimeSpan.FromSeconds(30))
            .RemoveAllLoggers()
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler {
                AllowAutoRedirect = false,
                UseCookies = false,
                UseProxy = false,
                ConnectTimeout = TimeSpan.FromSeconds(5),
                PooledConnectionLifetime = TimeSpan.FromMinutes(1),
                ConnectCallback = RecipeSourceReader.ConnectPublicAsync,
            });
        services.AddHttpClient<IRecipeSourceReader, RecipeSourceReader>(client => client.Timeout = TimeSpan.FromSeconds(20))
            .RemoveAllLoggers()
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler {
                AllowAutoRedirect = false,
                UseCookies = false,
                UseProxy = false,
                ConnectTimeout = TimeSpan.FromSeconds(5),
                PooledConnectionLifetime = TimeSpan.FromMinutes(1),
                ConnectCallback = RecipeSourceReader.ConnectPublicAsync,
            });
        services.AddOptions<OpenAiOptions>()
            .Bind(configuration.GetSection(OpenAiOptions.SectionName))
            .Validate(OpenAiOptions.HasTranscriptionModelWhenApiKeyConfigured,
                "OpenAi:TranscriptionModel is required when ApiKey is configured.")
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
