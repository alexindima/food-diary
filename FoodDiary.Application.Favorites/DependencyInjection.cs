using FluentValidation;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Application.Favorites.FavoriteMeals.Services;
using FoodDiary.Application.Favorites.FavoriteProducts.Services;
using FoodDiary.Application.Favorites.FavoriteRecipes.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Favorites;

public static class DependencyInjection {
    public static IServiceCollection AddFavoritesModule(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddScoped<IFavoriteMealReadService, FavoriteMealReadService>();
        services.AddScoped<IMealFavoriteReadService>(static provider =>
            (IMealFavoriteReadService)provider.GetRequiredService<IFavoriteMealReadService>());
        services.AddScoped<IFavoriteProductReadService, FavoriteProductReadService>();
        services.AddScoped<IFavoriteRecipeReadService, FavoriteRecipeReadService>();

        return services;
    }
}
