using FluentValidation;
using FoodDiary.Application.MealPlanning.MealPlans.Common;
using FoodDiary.Application.MealPlanning.MealPlans.Services;
using FoodDiary.Application.MealPlanning.ShoppingLists.Common;
using FoodDiary.Application.MealPlanning.ShoppingLists.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.MealPlanning;

public static class DependencyInjection {
    public static IServiceCollection AddMealPlanningModule(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddScoped<IMealPlanReadService, MealPlanReadService>();
        services.AddScoped<IShoppingListCreationService, ShoppingListCreationService>();
        services.AddScoped<IShoppingListReadService, ShoppingListReadService>();

        return services;
    }
}
