using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Infrastructure.Persistence.FavoriteMeals;
using FoodDiary.Infrastructure.Persistence.FavoriteProducts;
using FoodDiary.Infrastructure.Persistence.FavoriteRecipes;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static IServiceCollection AddFavoritesPersistence(this IServiceCollection services) {
        services.AddScoped<IFavoriteMealRepository, FavoriteMealRepository>();
        services.AddScoped<IFavoriteMealReadRepository>(static provider => provider.GetRequiredService<IFavoriteMealRepository>());
        services.AddScoped<IFavoriteMealReadModelRepository>(static provider => provider.GetRequiredService<IFavoriteMealRepository>());
        services.AddScoped<IFavoriteMealWriteRepository>(static provider => provider.GetRequiredService<IFavoriteMealRepository>());
        services.AddScoped<IFavoriteProductRepository, FavoriteProductRepository>();
        services.AddScoped<IFavoriteProductReadRepository>(static provider => provider.GetRequiredService<IFavoriteProductRepository>());
        services.AddScoped<IFavoriteProductReadModelRepository>(static provider => provider.GetRequiredService<IFavoriteProductRepository>());
        services.AddScoped<IFavoriteProductWriteRepository>(static provider => provider.GetRequiredService<IFavoriteProductRepository>());
        services.AddScoped<IFavoriteRecipeRepository, FavoriteRecipeRepository>();
        services.AddScoped<IFavoriteRecipeReadRepository>(static provider => provider.GetRequiredService<IFavoriteRecipeRepository>());
        services.AddScoped<IFavoriteRecipeReadModelRepository>(static provider => provider.GetRequiredService<IFavoriteRecipeRepository>());
        services.AddScoped<IFavoriteRecipeWriteRepository>(static provider => provider.GetRequiredService<IFavoriteRecipeRepository>());

        return services;
    }
}
