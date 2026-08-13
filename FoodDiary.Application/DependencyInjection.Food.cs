using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Consumptions.Services;
using FoodDiary.Application.Export.Services;
using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.MealPlans.Common;
using FoodDiary.Application.MealPlans.Services;
using FoodDiary.Application.OpenFoodFacts.Common;
using FoodDiary.Application.OpenFoodFacts.Services;
using FoodDiary.Application.Products.Common;
using FoodDiary.Application.Products.SearchSuggestions;
using FoodDiary.Application.Products.Services;
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
        services.AddScoped<IFavoriteMealSourceReadService>(static provider =>
            provider.GetRequiredService<IConsumptionReadService>() as IFavoriteMealSourceReadService
            ?? throw new InvalidOperationException($"{nameof(IConsumptionReadService)} must implement {nameof(IFavoriteMealSourceReadService)}."));
        services.AddScoped<IMealActivityReadService, MealActivityReadService>();
        services.AddScoped<IConsumptionExportReadService, ConsumptionExportReadService>();
        services.AddScoped<IMealProductNutritionReadService, MealProductNutritionReadService>();
        services.AddScoped<IMealNutritionService, MealNutritionService>();
        services.AddScoped<IMealPlanReadService, MealPlanReadService>();
        services.AddScoped<IShoppingListCreationService, ShoppingListCreationService>();
        services.AddScoped<IShoppingListReadService, ShoppingListReadService>();
        services.AddScoped<IExportDiaryReadService, ExportDiaryReadService>();
        services.AddScoped<IOpenFoodFactsCachedProductSearch, OpenFoodFactsCachedProductSearch>();
        services.AddScoped<IProductSearchSuggestionProvider, OpenFoodFactsProductSearchSuggestionProvider>();
        services.AddScoped<IProductSearchSuggestionProvider, UsdaProductSearchSuggestionProvider>();
        services.AddScoped<IRecentProductReadService, RecentProductReadService>();
        services.AddScoped<IRecentRecipeReadService, RecentRecipeReadService>();
        services.AddScoped<IUsdaDailyMicronutrientReadService, UsdaDailyMicronutrientReadService>();
        services.AddScoped<IUsdaFoodReadService, UsdaFoodReadService>();
        services.AddScoped<IUsdaProductSuggestionReadService, UsdaProductSuggestionReadService>();
    }
}
