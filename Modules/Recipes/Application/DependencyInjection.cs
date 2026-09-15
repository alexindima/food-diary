using FluentValidation;
using FoodDiary.Modules.Recipes.Application.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Recipes.Application;

public static class DependencyInjection {
    public static IServiceCollection AddRecipesApplication(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<RecentRecipeLoader>();
        return services;
    }
}
