using FoodDiary.Application.Abstractions.OpenFoodFacts.Common;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Integrations.Options;
using FoodDiary.Integrations.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace FoodDiary.Integrations;

public static partial class DependencyInjection {
    private static IServiceCollection AddFoodDataIntegrations(this IServiceCollection services, IConfiguration configuration) {
        services.AddHttpClient<IUsdaFoodSearchService, UsdaFoodSearchService>(client => {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddHttpClient<IOpenFoodFactsService, OpenFoodFactsService>(client => {
            OpenFoodFactsApiOptions openFoodFactsOptions = configuration
                .GetSection(OpenFoodFactsApiOptions.SectionName)
                .Get<OpenFoodFactsApiOptions>() ?? new OpenFoodFactsApiOptions();
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(openFoodFactsOptions.UserAgent);
        })
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
