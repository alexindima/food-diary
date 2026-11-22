using FoodDiary.Modules.Recipes.Application;
using FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;
using FoodDiary.Modules.Recipes.Contracts.Moderation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Modules.Recipes.Infrastructure.Moderation;

public static class RecipePublicationRegistration {
    internal static IServiceCollection AddRecipePublicationPersistence(this IServiceCollection services) {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<RecipePublicationStore>();
        services.AddScoped<IRecipePublicationQueue>(provider => provider.GetRequiredService<RecipePublicationStore>());
        services.AddScoped<IRecipeModerationAdministration, RecipeModerationAdministration>();
        return services;
    }

    public static IServiceCollection AddRecipePublicationWorker(this IServiceCollection services, IConfiguration configuration) {
        services.AddRecipePublicationApplication();
        services.AddRecipeModerationProviders(configuration);
        services.AddScoped<IRecipeModerationProcessor, RecipeModerationProcessor>();
        return services;
    }
}
