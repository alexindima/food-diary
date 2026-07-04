using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Infrastructure.Persistence.Meals;
using FoodDiary.Infrastructure.Persistence.Products;
using FoodDiary.Infrastructure.Persistence.RecentItems;
using FoodDiary.Infrastructure.Persistence.Recipes;
using FoodDiary.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static IServiceCollection AddFoodPersistence(this IServiceCollection services) {
        services.AddScoped<ProductRepository>();
        services.AddScoped<IProductRepository, CachedProductRepository>();
        services.AddScoped<IProductReadRepository>(static provider => provider.GetRequiredService<IProductRepository>());
        services.AddScoped<IProductWriteRepository>(static provider => provider.GetRequiredService<IProductRepository>());
        services.AddScoped<IProductLookupService, ProductLookupService>();

        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IRecipeReadRepository>(static provider => provider.GetRequiredService<IRecipeRepository>());
        services.AddScoped<IRecipeWriteRepository>(static provider => provider.GetRequiredService<IRecipeRepository>());
        services.AddScoped<IRecipeNutritionWriter>(static provider => provider.GetRequiredService<IRecipeRepository>());
        services.AddScoped<IRecipeLookupService, RecipeLookupService>();
        services.AddScoped<IRecipeAccessService, RecipeAccessService>();

        services.AddScoped<IRecentItemRepository, RecentItemRepository>();
        services.AddScoped<IRecentItemReadRepository>(static provider => provider.GetRequiredService<IRecentItemRepository>());
        services.AddScoped<IRecentItemWriteRepository>(static provider => provider.GetRequiredService<IRecentItemRepository>());

        services.AddScoped<IMealRepository, MealRepository>();
        services.AddScoped<IMealReadRepository>(static provider => provider.GetRequiredService<IMealRepository>());
        services.AddScoped<IMealWriteRepository>(static provider => provider.GetRequiredService<IMealRepository>());

        return services;
    }
}
