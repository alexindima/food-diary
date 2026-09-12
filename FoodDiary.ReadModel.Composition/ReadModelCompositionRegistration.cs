using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Infrastructure.Persistence.Admin;
using FoodDiary.Infrastructure.Persistence.Meals;
using FoodDiary.Infrastructure.Persistence.Recipes;
using FoodDiary.Modules.Dashboard.Infrastructure.Persistence.Dashboard;
using FoodDiary.Modules.Favorites.Infrastructure.Persistence.FavoriteMeals;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.ReadModel.Composition;

public static class ReadModelCompositionRegistration {
    public static IServiceCollection AddReadModelComposition(this IServiceCollection services) {
        services.AddScoped<IAdminDashboardMetricsReader, AdminDashboardMetricsReader>();
        services.AddScoped<IAdminRetentionReader, AdminRetentionReader>();
        services.AddScoped<IAdminUserRoleAuditRepository, AdminUserRoleAuditRepository>();
        services.AddScoped<IAdminUserRoleAuditReadRepository>(static provider => provider.GetRequiredService<IAdminUserRoleAuditRepository>());
        services.AddScoped<IAdminBillingRepository, AdminBillingRepository>();
        services.AddScoped<IAdminBillingReadRepository>(static provider => provider.GetRequiredService<IAdminBillingRepository>());
        services.AddScoped<IAdminImpersonationSessionQuery, AdminImpersonationSessionQuery>();
        services.AddScoped<IFavoriteMealQuery, FavoriteMealQuery>();
        services.AddScoped<IMealProductNutritionQuery, MealProductNutritionQuery>();
        services.AddScoped<IMealItemDisplayReadService, MealItemDisplayReadService>();
        services.AddScoped<IRecipeOverviewReadService, RecipeOverviewReadService>();
        services.RemoveAll<IDashboardMealsReadService>();
        services.AddScoped<DashboardMealsReadService>();
        services.AddScoped<IDashboardMealsReadService>(static provider => provider.GetRequiredService<DashboardMealsReadService>());
        return services;
    }
}
