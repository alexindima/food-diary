using FoodDiary.Application.Abstractions.OpenFoodFacts.Common;
using FoodDiary.Infrastructure.Persistence.OpenFoodFacts;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static void AddProviderCachePersistence(this IServiceCollection services) {
        services.AddScoped<IOpenFoodFactsProductCacheRepository, OpenFoodFactsProductCacheRepository>();
        services.AddScoped<IOpenFoodFactsProductCacheReadRepository>(static provider => provider.GetRequiredService<IOpenFoodFactsProductCacheRepository>());
        services.AddScoped<IOpenFoodFactsProductCacheWriteRepository>(static provider => provider.GetRequiredService<IOpenFoodFactsProductCacheRepository>());

    }
}
