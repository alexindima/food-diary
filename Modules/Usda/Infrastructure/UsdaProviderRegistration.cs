using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Integrations.Options;
using FoodDiary.Integrations.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Modules.Usda.Infrastructure;

public static class UsdaProviderRegistration {
    public static IServiceCollection AddUsdaProvider(this IServiceCollection services, IConfiguration configuration) {
        services.AddOptions<UsdaApiOptions>()
            .Bind(configuration.GetSection(UsdaApiOptions.SectionName))
            .Validate(UsdaApiOptions.HasValidBaseUrl,
                "UsdaApi:BaseUrl must be an absolute HTTPS URL.")
            .ValidateOnStart();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<UsdaFoodDetailCache>();
        services.AddHttpClient<IUsdaFoodSearchService, UsdaFoodSearchService>(client => client.Timeout = TimeSpan.FromSeconds(15))
            .RemoveAllLoggers();

        return services;
    }
}
