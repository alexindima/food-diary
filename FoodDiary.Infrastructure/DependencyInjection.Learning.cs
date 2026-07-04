using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Application.Abstractions.MealPlans.Common;
using FoodDiary.Infrastructure.Persistence.Content;
using FoodDiary.Infrastructure.Persistence.MealPlans;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static IServiceCollection AddLearningPersistence(this IServiceCollection services) {
        services.AddScoped<INutritionLessonRepository, NutritionLessonRepository>();
        services.AddScoped<INutritionLessonReadRepository>(static provider => provider.GetRequiredService<INutritionLessonRepository>());
        services.AddScoped<INutritionLessonWriteRepository>(static provider => provider.GetRequiredService<INutritionLessonRepository>());
        services.AddScoped<IMealPlanRepository, MealPlanRepository>();
        services.AddScoped<IMealPlanReadRepository>(static provider => provider.GetRequiredService<IMealPlanRepository>());
        services.AddScoped<IMealPlanWriteRepository>(static provider => provider.GetRequiredService<IMealPlanRepository>());

        return services;
    }
}
