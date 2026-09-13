using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Favorites.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Application.Favorites;
using FoodDiary.Modules.Favorites.Infrastructure.Persistence.FavoriteMeals;
using FoodDiary.Modules.Favorites.Infrastructure.Persistence.FavoriteProducts;
using FoodDiary.Modules.Favorites.Infrastructure.Persistence.FavoriteRecipes;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Favorites.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddFavoritesModule(this IServiceCollection services) {
        services.AddFavoritesApplication();
        services.AddScoped(provider => provider.GetRequiredService<FoodDiaryDbContext>()
            .CreateModuleContext<FavoritesDbContext>(options => new FavoritesDbContext(options)));
        services.AddScoped<IFavoriteMealRepository>(provider => new FavoriteMealRepository(
            provider.GetRequiredService<FavoritesDbContext>().FavoriteMeals, provider.GetRequiredService<IFavoriteMealQuery>()));
        services.AddScoped<IFavoriteMealReadRepository>(static provider => provider.GetRequiredService<IFavoriteMealRepository>());
        services.AddScoped<IFavoriteMealReadModelRepository>(static provider => provider.GetRequiredService<IFavoriteMealRepository>());
        services.AddScoped<IFavoriteMealWriteRepository>(static provider => provider.GetRequiredService<IFavoriteMealRepository>());
        services.AddScoped<IFavoriteProductRepository>(provider => new FavoriteProductRepository(
            provider.GetRequiredService<FavoritesDbContext>().FavoriteProducts, provider.GetRequiredService<IFavoriteProductQuery>()));
        services.AddScoped<IFavoriteProductReadRepository>(static provider => provider.GetRequiredService<IFavoriteProductRepository>());
        services.AddScoped<IFavoriteProductReadModelRepository>(static provider => provider.GetRequiredService<IFavoriteProductRepository>());
        services.AddScoped<IFavoriteProductWriteRepository>(static provider => provider.GetRequiredService<IFavoriteProductRepository>());
        services.AddScoped<IFavoriteRecipeRepository>(provider => new FavoriteRecipeRepository(
            provider.GetRequiredService<FavoritesDbContext>().FavoriteRecipes, provider.GetRequiredService<IFavoriteRecipeQuery>()));
        services.AddScoped<IFavoriteRecipeReadRepository>(static provider => provider.GetRequiredService<IFavoriteRecipeRepository>());
        services.AddScoped<IFavoriteRecipeReadModelRepository>(static provider => provider.GetRequiredService<IFavoriteRecipeRepository>());
        services.AddScoped<IFavoriteRecipeWriteRepository>(static provider => provider.GetRequiredService<IFavoriteRecipeRepository>());
        return services;
    }
}
