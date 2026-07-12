using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Infrastructure.Persistence.Meals;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static IServiceCollection AddMealsPersistence(this IServiceCollection services) {
        services.AddScoped<IMealRepository, MealRepository>();
        services.AddScoped<IMealReadRepository>(static provider => provider.GetRequiredService<IMealRepository>());
        services.AddScoped<IMealConsumptionReadRepository>(static provider => provider.GetRequiredService<IMealRepository>());
        services.AddScoped<IMealActivityReadRepository>(static provider => provider.GetRequiredService<IMealRepository>());
        services.AddScoped<IMealProductNutritionReadRepository>(static provider => provider.GetRequiredService<IMealRepository>());
        services.AddScoped<IMealWriteRepository>(static provider => provider.GetRequiredService<IMealRepository>());

        return services;
    }
}
