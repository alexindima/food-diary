using FoodDiary.Application.Abstractions.MealPlans.Common;
using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Application.MealPlanning;
using FoodDiary.Infrastructure.Persistence.MealPlans;
using FoodDiary.Infrastructure.Persistence.ShoppingLists;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.MealPlanning.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddMealPlanningModule(this IServiceCollection services) {
        services.AddMealPlanningApplication();
        services.AddScoped<IMealPlanRepository, MealPlanRepository>();
        services.AddScoped<IMealPlanReadRepository>(static provider => provider.GetRequiredService<IMealPlanRepository>());
        services.AddScoped<IMealPlanReadModelRepository>(static provider => provider.GetRequiredService<IMealPlanRepository>());
        services.AddScoped<IMealPlanWriteRepository>(static provider => provider.GetRequiredService<IMealPlanRepository>());
        services.AddScoped<IShoppingListRepository, ShoppingListRepository>();
        services.AddScoped<IShoppingListReadRepository>(static provider => provider.GetRequiredService<IShoppingListRepository>());
        services.AddScoped<IShoppingListReadModelRepository>(static provider => provider.GetRequiredService<IShoppingListRepository>());
        services.AddScoped<IShoppingListWriteRepository>(static provider => provider.GetRequiredService<IShoppingListRepository>());
        return services;
    }
}
