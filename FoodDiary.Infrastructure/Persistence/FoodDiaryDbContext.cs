using FoodDiary.Infrastructure.Persistence.Composition;
using Microsoft.EntityFrameworkCore;
using FoodDiary.Modules.MealPlanning.Infrastructure.Model;
using FoodDiary.Infrastructure.Persistence.Audit;
using FoodDiary.Infrastructure.Persistence.Email;
using FoodDiary.Infrastructure.Persistence.Outbox;
using FoodDiary.Modules.Fasting.Infrastructure.Persistence;
using FoodDiary.Modules.Hydration.Infrastructure.Persistence;
using FoodDiary.Modules.DailyAdvices.Infrastructure.Persistence;
using FoodDiary.Modules.Lessons.Infrastructure.Persistence;
using FoodDiary.Modules.ContentReports.Infrastructure.Persistence;
using FoodDiary.Modules.Favorites.Infrastructure.Persistence;
using FoodDiary.Modules.WeeklyGoals.Infrastructure.Persistence;
using FoodDiary.Modules.Cycles.Infrastructure.Persistence;
using FoodDiary.Modules.OpenFoodFacts.Infrastructure.Model;
using FoodDiary.Modules.Marketing.Infrastructure.Persistence;
using FoodDiary.Modules.Billing.Infrastructure.Persistence;
using FoodDiary.Modules.Usda.Infrastructure.Model;
using FoodDiary.Modules.BodyMetrics.Infrastructure.Persistence;
using FoodDiary.Modules.Notifications.Infrastructure.Model;

namespace FoodDiary.Infrastructure.Persistence;

public sealed partial class FoodDiaryDbContext(DbContextOptions<FoodDiaryDbContext> options) : DbContext(options) {
    internal DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyAdminPersistenceModel();
        modelBuilder.ApplyAiPersistenceModel();
        modelBuilder.ApplyUsersPersistenceModel();
        modelBuilder.ApplyIdentityPersistenceModel();
        modelBuilder.ApplyAuditPersistenceModel();
        modelBuilder.ApplyEmailPersistenceModel();
        modelBuilder.ApplyOutboxPersistenceModel();
        modelBuilder.ApplyWearablesPersistenceModel();
        modelBuilder.ApplyFastingPersistenceModel();
        modelBuilder.ApplyHydrationPersistenceModel();
        modelBuilder.ApplyDailyAdvicesPersistenceModel();
        modelBuilder.ApplyLessonsPersistenceModel();
        modelBuilder.ApplyContentReportsPersistenceModel();
        modelBuilder.ApplyFavoritesPersistenceModel();
        modelBuilder.ApplyWeeklyGoalsPersistenceModel();
        modelBuilder.ApplyGamificationPersistenceModel();
        modelBuilder.ApplyImagesPersistenceModel();
        modelBuilder.ApplyDietologistPersistenceModel();
        modelBuilder.ApplyCyclesPersistenceModel();
        modelBuilder.ApplyOpenFoodFactsPersistenceModel();
        modelBuilder.ApplyMarketingPersistenceModel();
        modelBuilder.ApplyBillingPersistenceModel();
        modelBuilder.ApplyUsdaPersistenceModel();
        modelBuilder.ApplyBodyMetricsPersistenceModel();
        modelBuilder.ApplyNotificationsPersistenceModel();
        modelBuilder.ApplyMealPlanningPersistenceModel();
        modelBuilder.ApplyExercisesPersistenceModel();
        modelBuilder.ApplyRecipeCommunityPersistenceModel();
        modelBuilder.ApplyRecipesPersistenceModel();
        modelBuilder.ApplyProductsPersistenceModel();
        modelBuilder.ApplyMealsPersistenceModel();
        modelBuilder.ApplyRecentItemsPersistenceModel();
        AiCrossModuleRelationships.Configure(modelBuilder);
        HydrationCrossModuleRelationships.Configure(modelBuilder);
        RecentItemsCrossModuleRelationships.Configure(modelBuilder);
        ExercisesCrossModuleRelationships.Configure(modelBuilder);
        WeeklyGoalsCrossModuleRelationships.Configure(modelBuilder);
    }
}
