using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Infrastructure.Persistence.Usda;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static IServiceCollection AddUsdaPersistence(this IServiceCollection services) {
        services.AddScoped<IUsdaFoodRepository, UsdaFoodRepository>();
        services.AddScoped<IUsdaFoodReadRepository>(static provider => provider.GetRequiredService<IUsdaFoodRepository>());
        services.AddScoped<IUsdaFoodReadModelRepository>(static provider => provider.GetRequiredService<IUsdaFoodRepository>());
        services.AddScoped<IUsdaProductLinkRepository, UsdaProductLinkRepository>();
        services.AddScoped<IUsdaProductLinkReadRepository>(static provider => provider.GetRequiredService<IUsdaProductLinkRepository>());
        services.AddScoped<IUsdaProductLinkWriteRepository>(static provider => provider.GetRequiredService<IUsdaProductLinkRepository>());

        return services;
    }
}
