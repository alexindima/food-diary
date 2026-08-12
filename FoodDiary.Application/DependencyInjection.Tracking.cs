using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Cycles.Common;
using FoodDiary.Application.Cycles.Services;
using FoodDiary.Application.Dashboard.Common;
using FoodDiary.Application.Dashboard.Services;
using FoodDiary.Application.Exercises.Common;
using FoodDiary.Application.Exercises.Services;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Services;
using FoodDiary.Application.Gamification.Common;
using FoodDiary.Application.Gamification.Services;
using FoodDiary.Application.Hydration.Common;
using FoodDiary.Application.Hydration.Services;
using FoodDiary.Application.Tdee.Common;
using FoodDiary.Application.Tdee.Services;
using FoodDiary.Application.Wearables.Common;
using FoodDiary.Application.Wearables.Services;
using FoodDiary.Application.WeeklyCheckIn.Common;
using FoodDiary.Application.WeeklyCheckIn.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Application;

public static partial class DependencyInjection {
    private static void AddTrackingModules(this IServiceCollection services) {
        services.AddScoped<ICycleReadService, CycleReadService>();
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
        services.AddScoped<IFastingAnalyticsService, FastingAnalyticsService>();
        services.AddScoped<IFastingReadService, FastingReadService>();
        services.AddScoped<IFastingTelemetrySummaryReadService, FastingTelemetrySummaryReadService>();
        services.AddScoped<IFastingNotificationScheduler, FastingNotificationScheduler>();
        services.AddScoped<IHydrationEntryReadService, HydrationEntryReadService>();
        services.AddScoped<IHydrationGoalService, HydrationGoalService>();
        services.AddScoped<IGamificationReadService, GamificationReadService>();
        services.AddScoped<IGamificationUserProfileService, GamificationUserProfileService>();
        services.AddScoped<IAchievementAwardService, AchievementAwardService>();
        services.AddScoped<IAchievementReconciliationHandler, AchievementReconciliationHandler>();
        services.AddScoped<IAchievementDefinitionAdministrationService, AchievementDefinitionAdministrationService>();
        services.AddScoped<IExerciseEntryReadService, ExerciseEntryReadService>();
        services.AddScoped<IWearableReadService, WearableReadService>();
        services.AddScoped<ITdeeUserProfileService, TdeeUserProfileService>();
        services.AddScoped<IWeeklyCheckInUserProfileService, WeeklyCheckInUserProfileService>();
        services.AddScoped<IWeeklyCheckInReadService, WeeklyCheckInReadService>();
    }
}
