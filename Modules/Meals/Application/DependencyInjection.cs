using FoodDiary.Modules.Usda.Contracts.Common;
using FluentValidation;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Common;
using FoodDiary.Modules.Meals.Application.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Meals.Application;

public static class DependencyInjection {
    public static IServiceCollection AddMealsApplication(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<IFavoriteMealSourceReadService, FavoriteMealSourceReadService>();
        services.AddScoped<IUsdaMealNutritionReadService, MealProductNutritionReadService>();
        services.AddScoped<IMealNutritionService, MealNutritionService>();
        return services;
    }
}
