using FoodDiary.Persistence.Abstractions;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Common;
using FoodDiary.Application.OpenFoodFacts;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;
using FoodDiary.Modules.OpenFoodFacts.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.OpenFoodFacts.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddOpenFoodFactsModule(this IServiceCollection services) {
        services.AddOpenFoodFactsApplication();
        services.AddScoped(static provider => provider.GetRequiredService<IModuleContextFactory>()
            .CreateModuleContext<OpenFoodFactsDbContext>(static options => new OpenFoodFactsDbContext(options)));
        services.AddScoped<IOpenFoodFactsProductCacheRepository>(static provider => {
            FoodDiaryDbContext shared = provider.GetRequiredService<FoodDiaryDbContext>();
            return new OpenFoodFactsProductCacheRepository(
                provider.GetRequiredService<OpenFoodFactsDbContext>(),
                () => shared.Database.CurrentTransaction?.GetDbTransaction(),
                provider.GetRequiredService<TimeProvider>());
        });
        services.AddScoped<IOpenFoodFactsProductCacheReadRepository>(static provider => provider.GetRequiredService<IOpenFoodFactsProductCacheRepository>());
        services.AddScoped<IOpenFoodFactsProductCacheWriteRepository>(static provider => provider.GetRequiredService<IOpenFoodFactsProductCacheRepository>());
        return services;
    }
}
