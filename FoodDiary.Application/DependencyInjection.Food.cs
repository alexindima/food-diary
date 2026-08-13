using FoodDiary.Application.Products.Common;
using FoodDiary.Application.Products.SearchSuggestions;
using FoodDiary.Application.Products.Services;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Application.Recipes.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application;

public static partial class DependencyInjection {
    private static void AddFoodModules(this IServiceCollection services) {
        services.AddScoped<IProductSearchSuggestionProvider, OpenFoodFactsProductSearchSuggestionProvider>();
        services.AddScoped<IProductSearchSuggestionProvider, UsdaProductSearchSuggestionProvider>();
        services.AddScoped<IRecentProductReadService, RecentProductReadService>();
        services.AddScoped<IRecentRecipeReadService, RecentRecipeReadService>();
    }
}
