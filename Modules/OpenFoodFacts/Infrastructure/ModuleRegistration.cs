using FoodDiary.Application.Abstractions.OpenFoodFacts.Common;
using FoodDiary.Application.OpenFoodFacts;
using FoodDiary.Modules.OpenFoodFacts.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.OpenFoodFacts.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddOpenFoodFactsModule(this IServiceCollection services) {
        services.AddOpenFoodFactsApplication();
        services.AddScoped<IOpenFoodFactsProductCacheRepository, OpenFoodFactsProductCacheRepository>();
        services.AddScoped<IOpenFoodFactsProductCacheReadRepository>(static provider => provider.GetRequiredService<IOpenFoodFactsProductCacheRepository>());
        services.AddScoped<IOpenFoodFactsProductCacheWriteRepository>(static provider => provider.GetRequiredService<IOpenFoodFactsProductCacheRepository>());
        return services;
    }
}
