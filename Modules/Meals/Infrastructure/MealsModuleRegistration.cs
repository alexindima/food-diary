using Microsoft.Extensions.DependencyInjection.Extensions;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Meals;
using FoodDiary.Infrastructure.Persistence.Meals;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static class MealsModuleRegistration {
    public static IServiceCollection AddMealsModule(this IServiceCollection services) =>
        services.AddMealsApplication().AddMealsPersistence();

    public static IServiceCollection AddMealsPersistence(this IServiceCollection services) {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IUserDataPurgeParticipant, MealsUserDataPurgeParticipant>());
        services.AddScoped<IMealDailyCalorieReadService, MealDailyCalorieReadService>();
        services.AddScoped<IMealNutritionStatisticsReadService, MealNutritionStatisticsReadService>();
        services.AddScoped<IMealRepository, MealRepository>();
        services.AddScoped<IMealRecognitionTransactionRunner, EfMealRecognitionTransactionRunner>();
        services.AddScoped<IMealRecognitionReceiptRepository, MealRecognitionReceiptRepository>();
        services.AddScoped<IMealReadRepository>(static provider => provider.GetRequiredService<IMealRepository>());
        services.AddScoped<IMealProjectionReadRepository>(static provider => provider.GetRequiredService<IMealRepository>());
        services.AddScoped<IMealActivityReadRepository>(static provider => provider.GetRequiredService<IMealRepository>());
        services.AddScoped<IMealProductNutritionReadRepository>(static provider => provider.GetRequiredService<IMealRepository>());
        services.AddScoped<IMealWriteRepository>(static provider => provider.GetRequiredService<IMealRepository>());

        return services;
    }
}
