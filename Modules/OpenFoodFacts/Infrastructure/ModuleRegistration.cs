using FoodDiary.Persistence.Abstractions;
using FoodDiary.Modules.OpenFoodFacts.Application.Abstractions.Common;
using FoodDiary.Modules.OpenFoodFacts.Application;
using FoodDiary.Modules.OpenFoodFacts.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.OpenFoodFacts.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddOpenFoodFactsModule(this IServiceCollection services) {
        services.AddOpenFoodFactsApplication();
        services.AddScoped(static provider => provider.GetRequiredService<IModuleContextFactory>()
            .CreateModuleContext<OpenFoodFactsDbContext>(static options => new OpenFoodFactsDbContext(options)));
        services.AddScoped<IOpenFoodFactsProductCacheRepository>(static provider => {
            IModuleTransactionCoordinator coordinator = provider.GetRequiredService<IModuleTransactionCoordinator>();
            return new OpenFoodFactsProductCacheRepository(
                provider.GetRequiredService<OpenFoodFactsDbContext>(),
                () => coordinator.CurrentTransaction,
                provider.GetRequiredService<TimeProvider>());
        });
        services.AddScoped<IOpenFoodFactsProductCacheReadRepository>(static provider => provider.GetRequiredService<IOpenFoodFactsProductCacheRepository>());
        services.AddScoped<IOpenFoodFactsProductCacheWriteRepository>(static provider => provider.GetRequiredService<IOpenFoodFactsProductCacheRepository>());
        return services;
    }
}
