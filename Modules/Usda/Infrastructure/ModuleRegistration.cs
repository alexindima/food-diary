using FoodDiary.Persistence.Abstractions;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Usda;
using FoodDiary.Modules.Usda.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Usda.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddUsdaModule(this IServiceCollection services) {
        services.AddUsdaApplication();
        services.AddScoped(static provider => provider.GetRequiredService<IModuleContextFactory>()
            .CreateModuleContext<UsdaDbContext>(static options => new UsdaDbContext(options)));
        services.AddScoped<IUsdaFoodRepository>(static provider => {
            UsdaDbContext context = provider.GetRequiredService<UsdaDbContext>();
            return new UsdaFoodRepository(context.UsdaFoods, context.UsdaFoodNutrients,
                context.UsdaFoodPortions, context.DailyReferenceValues);
        });
        services.AddScoped<IUsdaFoodReadRepository>(static provider => provider.GetRequiredService<IUsdaFoodRepository>());
        services.AddScoped<IUsdaFoodReadModelRepository>(static provider => provider.GetRequiredService<IUsdaFoodRepository>());
        return services;
    }
}
