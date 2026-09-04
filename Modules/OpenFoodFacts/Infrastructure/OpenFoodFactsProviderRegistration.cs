using FoodDiary.Application.Abstractions.OpenFoodFacts.Common;
using FoodDiary.Integrations.Options;
using FoodDiary.Integrations.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace FoodDiary.Modules.OpenFoodFacts.Infrastructure;

public static class OpenFoodFactsProviderRegistration {
    public static IServiceCollection AddOpenFoodFactsProvider(this IServiceCollection services, IConfiguration configuration) {
        services.AddOptions<OpenFoodFactsApiOptions>()
            .Bind(configuration.GetSection(OpenFoodFactsApiOptions.SectionName))
            .Validate(OpenFoodFactsApiOptions.HasValidBaseUrl,
                "OpenFoodFacts:BaseUrl must be an absolute HTTPS URL.")
            .Validate(OpenFoodFactsApiOptions.HasValidUserAgent,
                "OpenFoodFacts:UserAgent must be a valid HTTP User-Agent value.")
            .ValidateOnStart();
        services.TryAddSingleton(TimeProvider.System);

        services.AddHttpClient<IOpenFoodFactsService, OpenFoodFactsService>(client => {
            OpenFoodFactsApiOptions openFoodFactsOptions = configuration
                .GetSection(OpenFoodFactsApiOptions.SectionName)
                .Get<OpenFoodFactsApiOptions>() ?? new OpenFoodFactsApiOptions();
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(openFoodFactsOptions.UserAgent);
        })
        .RemoveAllLoggers()
        .AddResilienceHandler("open-food-facts-retry", builder => {
            builder.AddRetry(new HttpRetryStrategyOptions {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromMilliseconds(250),
                BackoffType = DelayBackoffType.Exponential,
            });
        });

        return services;
    }
}
