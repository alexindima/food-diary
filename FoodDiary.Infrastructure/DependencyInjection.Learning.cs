using FoodDiary.Application.Abstractions.MealPlans.Common;
using FoodDiary.Infrastructure.Persistence.MealPlans;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static void AddLearningPersistence(this IServiceCollection services) {
        services.AddScoped<IMealPlanRepository, MealPlanRepository>();
        services.AddScoped<IMealPlanReadRepository>(static provider => provider.GetRequiredService<IMealPlanRepository>());
        services.AddScoped<IMealPlanReadModelRepository>(static provider => provider.GetRequiredService<IMealPlanRepository>());
        services.AddScoped<IMealPlanWriteRepository>(static provider => provider.GetRequiredService<IMealPlanRepository>());

    }
}
