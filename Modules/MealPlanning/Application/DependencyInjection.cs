using FluentValidation;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Common;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.MealPlanning.Application;

public static class DependencyInjection {
    public static IServiceCollection AddMealPlanningApplication(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddScoped<IShoppingListCreationService, ShoppingListCreationService>();

        return services;
    }
}
