using FluentValidation;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Application.Recipes.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Recipes;

public static class DependencyInjection {
    public static IServiceCollection AddRecipesModule(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<IRecentRecipeReadService, RecentRecipeReadService>();
        return services;
    }
}
