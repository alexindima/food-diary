using FluentValidation;
using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Application.Meals.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Meals;

public static class DependencyInjection {
    public static IServiceCollection AddMealsModule(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<IMealReadService, MealReadService>();
        services.AddScoped<IFavoriteMealSourceReadService>(static provider =>
            provider.GetRequiredService<IMealReadService>() as IFavoriteMealSourceReadService
            ?? throw new InvalidOperationException($"{nameof(IMealReadService)} must implement {nameof(IFavoriteMealSourceReadService)}."));
        services.AddScoped<IMealActivityReadService, MealActivityReadService>();
        services.AddScoped<IMealExportReadService, MealExportReadService>();
        services.AddScoped<IMealProductNutritionReadService, MealProductNutritionReadService>();
        services.AddScoped<IMealNutritionService, MealNutritionService>();
        return services;
    }
}
