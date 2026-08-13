using FoodDiary.Application.Recipes.Common;
using FoodDiary.Application.Recipes.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application;

public static partial class DependencyInjection {
    private static void AddFoodModules(this IServiceCollection services) {
        services.AddScoped<IRecentRecipeReadService, RecentRecipeReadService>();
    }
}
