using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Consumptions.Services;
using FoodDiary.Application.Export.Services;
using FoodDiary.Application.FavoriteMeals.Common;
using FoodDiary.Application.FavoriteMeals.Services;
using FoodDiary.Application.FavoriteProducts.Common;
using FoodDiary.Application.FavoriteProducts.Services;
using FoodDiary.Application.FavoriteRecipes.Common;
using FoodDiary.Application.FavoriteRecipes.Services;
using FoodDiary.Application.MealPlans.Common;
using FoodDiary.Application.MealPlans.Services;
using FoodDiary.Application.OpenFoodFacts.Common;
using FoodDiary.Application.OpenFoodFacts.Services;
using FoodDiary.Application.Products.Common;
using FoodDiary.Application.Products.SearchSuggestions;
using FoodDiary.Application.Products.Services;
using FoodDiary.Application.RecipeComments.Common;
using FoodDiary.Application.RecipeComments.Services;
using FoodDiary.Application.RecipeLikes.Common;
using FoodDiary.Application.RecipeLikes.Services;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Application.Recipes.Services;
using FoodDiary.Application.ShoppingLists.Common;
using FoodDiary.Application.ShoppingLists.Services;
using FoodDiary.Application.Usda.Common;
using FoodDiary.Application.Usda.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application;

public static partial class DependencyInjection {
    private static void AddFoodModules(this IServiceCollection services) {
        services.AddScoped<IConsumptionReadService, ConsumptionReadService>();
        services.AddScoped<IMealActivityReadService, MealActivityReadService>();
        services.AddScoped<IConsumptionExportReadService, ConsumptionExportReadService>();
        services.AddScoped<IMealProductNutritionReadService, MealProductNutritionReadService>();
        services.AddScoped<IMealNutritionService, MealNutritionService>();
        services.AddScoped<IMealPlanReadService, MealPlanReadService>();
        services.AddScoped<IShoppingListCreationService, ShoppingListCreationService>();
        services.AddScoped<IFavoriteMealReadService, FavoriteMealReadService>();
        services.AddScoped<IConsumptionFavoriteReadService>(static provider =>
            (IConsumptionFavoriteReadService)provider.GetRequiredService<IFavoriteMealReadService>());
        services.AddScoped<IFavoriteProductReadService, FavoriteProductReadService>();
        services.AddScoped<IFavoriteRecipeReadService, FavoriteRecipeReadService>();
        services.AddScoped<IShoppingListReadService, ShoppingListReadService>();
        services.AddScoped<IExportDiaryReadService, ExportDiaryReadService>();
        services.AddScoped<IOpenFoodFactsCachedProductSearch, OpenFoodFactsCachedProductSearch>();
        services.AddScoped<IProductSearchSuggestionProvider, OpenFoodFactsProductSearchSuggestionProvider>();
        services.AddScoped<IProductSearchSuggestionProvider, UsdaProductSearchSuggestionProvider>();
        services.AddScoped<IRecipeCommentReadService, RecipeCommentReadService>();
        services.AddScoped<IRecipeLikeReadService, RecipeLikeReadService>();
        services.AddScoped<IRecentProductReadService, RecentProductReadService>();
        services.AddScoped<IRecentRecipeReadService, RecentRecipeReadService>();
        services.AddScoped<IUsdaDailyMicronutrientReadService, UsdaDailyMicronutrientReadService>();
        services.AddScoped<IUsdaFoodReadService, UsdaFoodReadService>();
        services.AddScoped<IUsdaProductSuggestionReadService, UsdaProductSuggestionReadService>();
    }
}
