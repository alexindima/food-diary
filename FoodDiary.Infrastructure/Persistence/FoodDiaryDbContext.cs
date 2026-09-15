using FoodDiary.Modules.RecentItems.PersistenceModel;
using FoodDiary.Modules.RecipeCommunity.PersistenceModel;
using FoodDiary.Modules.Recipes.PersistenceModel;
using FoodDiary.Modules.Products.PersistenceModel;
using FoodDiary.Modules.Meals.PersistenceModel;
using FoodDiary.Modules.Marketing.PersistenceModel;
using FoodDiary.Modules.Notifications.PersistenceModel;
using FoodDiary.Modules.MealPlanning.PersistenceModel;
using FoodDiary.Modules.OpenFoodFacts.PersistenceModel;
using FoodDiary.Modules.Images.PersistenceModel;
using FoodDiary.Modules.Gamification.PersistenceModel;
using FoodDiary.Modules.Lessons.PersistenceModel;
using FoodDiary.Modules.Hydration.PersistenceModel;
using FoodDiary.Modules.Identity.PersistenceModel;
using FoodDiary.Modules.Exercises.PersistenceModel;
using FoodDiary.Persistence.Runtime.Persistence;

using FoodDiary.Modules.Dietologist.PersistenceModel;
using FoodDiary.Modules.Identity.PersistenceModel.Authentication;
using FoodDiary.Modules.ContentReports.PersistenceModel;
using FoodDiary.Modules.BodyMetrics.PersistenceModel;
using FoodDiary.Modules.Billing.PersistenceModel;
using FoodDiary.Modules.Ai.PersistenceModel;
using FoodDiary.Modules.Admin.PersistenceModel;
using FoodDiary.Infrastructure.Persistence.Composition;
using Microsoft.EntityFrameworkCore;

using FoodDiary.Infrastructure.Persistence.Audit;
using FoodDiary.Infrastructure.Persistence.Email;
using FoodDiary.Infrastructure.Persistence.Outbox;
using FoodDiary.Modules.Fasting.PersistenceModel;

using FoodDiary.Modules.DailyAdvices.PersistenceModel;

using FoodDiary.Modules.Favorites.PersistenceModel;
using FoodDiary.Modules.WeeklyGoals.Infrastructure.Persistence;
using FoodDiary.Modules.Cycles.PersistenceModel;

using FoodDiary.Modules.Usda.Infrastructure.Model;

namespace FoodDiary.Infrastructure.Persistence;

public sealed partial class FoodDiaryDbContext(DbContextOptions<FoodDiaryDbContext> options) : SharedPersistenceDbContext(options) {
    internal DbSet<TelegramLoginTicket> TelegramLoginTickets => Set<TelegramLoginTicket>();
    internal DbSet<TelegramOperation> TelegramOperations => Set<TelegramOperation>();

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
        ConfigureCrossModuleRelationships(modelBuilder);
    }

    private static void ConfigureCrossModuleRelationships(ModelBuilder modelBuilder) {
        AiCrossModuleRelationships.Configure(modelBuilder);
        HydrationCrossModuleRelationships.Configure(modelBuilder);
        RecentItemsCrossModuleRelationships.Configure(modelBuilder);
        ExercisesCrossModuleRelationships.Configure(modelBuilder);
        WeeklyGoalsCrossModuleRelationships.Configure(modelBuilder);
        ImagesCrossModuleRelationships.Configure(modelBuilder);
        CyclesCrossModuleRelationships.Configure(modelBuilder);
        BodyMetricsCrossModuleRelationships.Configure(modelBuilder);
        WearablesCrossModuleRelationships.Configure(modelBuilder);
        ContentReportsCrossModuleRelationships.Configure(modelBuilder);
        LessonsCrossModuleRelationships.Configure(modelBuilder);
        GamificationCrossModuleRelationships.Configure(modelBuilder);
        NotificationsCrossModuleRelationships.Configure(modelBuilder);
        FastingCrossModuleRelationships.Configure(modelBuilder);
        BillingCrossModuleRelationships.Configure(modelBuilder);
        AdminCrossModuleRelationships.Configure(modelBuilder);
        IdentityCrossModuleRelationships.Configure(modelBuilder);
        RecipeCommunityCrossModuleRelationships.Configure(modelBuilder);
        DietologistCrossModuleRelationships.Configure(modelBuilder);
        UsersCrossModuleRelationships.Configure(modelBuilder);
        FavoritesCrossModuleRelationships.Configure(modelBuilder);
        MealPlanningCrossModuleRelationships.Configure(modelBuilder);
        ProductsCrossModuleRelationships.Configure(modelBuilder);
        RecipesCrossModuleRelationships.Configure(modelBuilder);
        MealsCrossModuleRelationships.Configure(modelBuilder);
    }
}
