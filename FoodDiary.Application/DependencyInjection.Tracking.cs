using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Application.Dashboard.Common;
using FoodDiary.Application.Dashboard.Services;
using FoodDiary.Application.Gamification.Common;
using FoodDiary.Application.Gamification.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Application;

public static partial class DependencyInjection {
    private static void AddTrackingModules(this IServiceCollection services) {
        services.TryAddScoped<IDashboardStatisticsReadService, MediatorDashboardStatisticsReadService>();
        services.TryAddScoped<IDashboardBodyReadService, RepositoryDashboardBodyReadService>();
        services.TryAddScoped<IDashboardMealsReadService, MediatorDashboardMealsReadService>();
        services.TryAddScoped<IDashboardReadService, ComposedDashboardReadService>();
        services.AddScoped<IDashboardUserContextService, DashboardUserContextService>();
        services.AddScoped<IDashboardSectionDataLoader, DashboardSectionDataLoader>();
        services.AddScoped<IDashboardSnapshotBuilder>(static serviceProvider =>
            new DashboardSnapshotBuilder(
                serviceProvider.GetRequiredService<IDashboardSectionDataLoader>(),
                serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DashboardSnapshotBuilder>>()));
        services.AddScoped<IGamificationReadService, GamificationReadService>();
        services.AddScoped<IGamificationUserProfileService, GamificationUserProfileService>();
        services.AddScoped<IAchievementAwardService, AchievementAwardService>();
        services.AddScoped<IAchievementReconciliationHandler, AchievementReconciliationHandler>();
        services.AddScoped<IAchievementDefinitionAdministrationService, AchievementDefinitionAdministrationService>();
    }
}
