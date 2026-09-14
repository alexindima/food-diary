using FoodDiary.Persistence.Abstractions;
using FoodDiary.Modules.Recipes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using FoodDiary.Application.Abstractions.Products.Common;
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
        services.AddScoped(provider => provider.GetRequiredService<IModuleContextFactory>()
            .CreateModuleContext<RecipesDbContext>(options => new RecipesDbContext(options)));
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IUserDataPurgeParticipant, RecipesUserDataPurgeParticipant>());
        services.AddScoped<IRecipeRepository>(provider => new RecipeRepository(
            provider.GetRequiredService<RecipesDbContext>(), provider.GetRequiredService<IProductSnapshotReadService>(),
            provider.GetRequiredService<IRecipeUsageQuery>(), CreateTransactionSynchronizer(provider)));
        services.AddScoped<IRecipeReadRepository>(static provider => provider.GetRequiredService<IRecipeRepository>());
        services.AddScoped<IRecipeWriteRepository>(static provider => provider.GetRequiredService<IRecipeRepository>());
        services.AddScoped<IRecipeNutritionWriter>(static provider => provider.GetRequiredService<IRecipeRepository>());
        services.AddScoped<IRecipeMutationTransactionRunner, EfRecipeMutationTransactionRunner>();
        services.AddScoped<IRecipeLookupService, RecipeLookupService>();
        services.AddScoped<IRecipeAccessService, RecipeAccessService>();
        services.AddScoped<IFavoriteRecipeSourceReadService, FavoriteRecipeSourceReadService>();
        return services;
    }

    private static Func<CancellationToken, Task> CreateTransactionSynchronizer(IServiceProvider provider) {
        IModuleTransactionCoordinator coordinator = provider.GetRequiredService<IModuleTransactionCoordinator>();
        RecipesDbContext owned = provider.GetRequiredService<RecipesDbContext>();
        return async cancellationToken => {
            if (owned.Database.IsRelational()) {
                await owned.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken).ConfigureAwait(false);
            }
        };
    }
}
