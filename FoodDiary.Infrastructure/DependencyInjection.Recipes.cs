using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Infrastructure.Persistence.Recipes;
using FoodDiary.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static IServiceCollection AddRecipesPersistence(this IServiceCollection services) {
        services.AddScoped<IRecipeOverviewReadService, RecipeOverviewReadService>();
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IRecipeReadRepository>(static provider => provider.GetRequiredService<IRecipeRepository>());
        services.AddScoped<IRecipeWriteRepository>(static provider => provider.GetRequiredService<IRecipeRepository>());
        services.AddScoped<IRecipeNutritionWriter>(static provider => provider.GetRequiredService<IRecipeRepository>());
        services.AddScoped<IRecipeLookupService, RecipeLookupService>();
        services.AddScoped<IRecipeAccessService, RecipeAccessService>();

        return services;
    }
}
