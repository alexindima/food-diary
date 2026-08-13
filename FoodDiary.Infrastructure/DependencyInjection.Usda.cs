using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Infrastructure.Persistence.Usda;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static void AddUsdaPersistence(this IServiceCollection services) {
        services.AddScoped<IUsdaFoodRepository, UsdaFoodRepository>();
        services.AddScoped<IUsdaFoodReadRepository>(static provider => provider.GetRequiredService<IUsdaFoodRepository>());
        services.AddScoped<IUsdaFoodReadModelRepository>(static provider => provider.GetRequiredService<IUsdaFoodRepository>());
    }
}
