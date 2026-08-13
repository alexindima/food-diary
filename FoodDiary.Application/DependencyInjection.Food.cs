using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Export.Services;
using FoodDiary.Application.Products.Common;
using FoodDiary.Application.Products.SearchSuggestions;
using FoodDiary.Application.Products.Services;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Application.Recipes.Services;
using FoodDiary.Application.Usda.Common;
using FoodDiary.Application.Usda.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application;

public static partial class DependencyInjection {
    private static void AddFoodModules(this IServiceCollection services) {
        services.AddScoped<IExportDiaryReadService, ExportDiaryReadService>();
        services.AddScoped<IProductSearchSuggestionProvider, OpenFoodFactsProductSearchSuggestionProvider>();
        services.AddScoped<IProductSearchSuggestionProvider, UsdaProductSearchSuggestionProvider>();
        services.AddScoped<IRecentProductReadService, RecentProductReadService>();
        services.AddScoped<IRecentRecipeReadService, RecentRecipeReadService>();
        services.AddScoped<IUsdaDailyMicronutrientReadService, UsdaDailyMicronutrientReadService>();
        services.AddScoped<IUsdaFoodReadService, UsdaFoodReadService>();
        services.AddScoped<IUsdaProductSuggestionReadService, UsdaProductSuggestionReadService>();
    }
}
