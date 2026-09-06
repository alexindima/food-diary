using FoodDiary.Application.Abstractions.FavoriteRecipes.Common;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Recipes;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Infrastructure.Persistence.Recipes;
using FoodDiary.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static class RecipesModuleRegistration {
    public static IServiceCollection AddRecipesModule(this IServiceCollection services) =>
        services.AddRecipesApplication().AddRecipesPersistence();

    public static IServiceCollection AddRecipesPersistence(this IServiceCollection services) {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IUserDataPurgeParticipant, RecipesUserDataPurgeParticipant>());
        services.AddScoped<IRecipeOverviewReadService, RecipeOverviewReadService>();
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IRecipeReadRepository>(static provider => provider.GetRequiredService<IRecipeRepository>());
        services.AddScoped<IRecipeWriteRepository>(static provider => provider.GetRequiredService<IRecipeRepository>());
        services.AddScoped<IRecipeNutritionWriter>(static provider => provider.GetRequiredService<IRecipeRepository>());
        services.AddScoped<IRecipeMutationTransactionRunner, EfRecipeMutationTransactionRunner>();
        services.AddScoped<IRecipeLookupService, RecipeLookupService>();
        services.AddScoped<IRecipeAccessService, RecipeAccessService>();
        services.AddScoped<IFavoriteRecipeSourceReadService, FavoriteRecipeSourceReadService>();
        return services;
    }
}
