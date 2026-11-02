using FoodDiary.Modules.Marketing.Domain.Entities.Tracking;
using FoodDiary.Modules.WeeklyGoals.Domain.Entities;
using FoodDiary.Modules.Wearables.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities.Tracking;
using FoodDiary.Modules.RecentItems.Domain.Entities.Recents;
using FoodDiary.Modules.Notifications.Domain.Entities;
using FoodDiary.Modules.Gamification.Domain.Entities.Achievements;
using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;
using FoodDiary.Modules.Exercises.Domain.Entities.Tracking;
using FoodDiary.Modules.Cycles.Domain.Entities;
using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Hydration.Domain.Entities.Tracking;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.Lessons.Domain.Entities.Content;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteMeals;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteProducts;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteRecipes;
using FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;
using FoodDiary.Modules.MealPlanning.Domain.Entities.Shopping;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.RecipeCommunity.Domain.Entities.Recipes;
using FoodDiary.Modules.RecipeCommunity.Domain.Entities.Social;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Admin.Domain.Entities;
using FoodDiary.Modules.Ai.Domain.Entities;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.BodyMetrics.Domain.Entities.Tracking;
using FoodDiary.Modules.ContentReports.Domain.Entities;
using FoodDiary.Modules.Dietologist.Domain.Entities;

namespace FoodDiary.Infrastructure.Persistence;

/// <summary>Only query surfaces for cross-module projections on the current persistence session.</summary>
public interface ICompositionReadContext {
    IQueryable<AdminImpersonationSession> AdminImpersonationSessions { get; }
    IQueryable<AiUsage> AiUsages { get; }
    IQueryable<ImageAsset> FoodRecognitionImageAssets { get; }
    IQueryable<BillingCancellation> BillingCancellations { get; }
    IQueryable<BillingPayment> BillingPayments { get; }
    IQueryable<BillingSubscription> BillingSubscriptions { get; }
    IQueryable<BillingWebhookEvent> BillingWebhookEvents { get; }
    IQueryable<ContentReport> ContentReports { get; }
    IQueryable<DietologistInvitation> DietologistInvitations { get; }
    IQueryable<FavoriteMeal> FavoriteMeals { get; }
    IQueryable<FavoriteProduct> FavoriteProducts { get; }
    IQueryable<FavoriteRecipe> FavoriteRecipes { get; }
    IQueryable<HydrationEntry> HydrationEntries { get; }
    IQueryable<ImageAsset> ImageAssets { get; }
    IQueryable<MealAiItem> MealAiItems { get; }
    IQueryable<MealAiSession> MealAiSessions { get; }
    IQueryable<MealItem> MealItems { get; }
    IQueryable<MealPlan> MealPlans { get; }
    IQueryable<Meal> Meals { get; }
    IQueryable<Product> Products { get; }
    IQueryable<RecipeComment> RecipeComments { get; }
    IQueryable<RecipeIngredient> RecipeIngredients { get; }
    IQueryable<RecipeLike> RecipeLikes { get; }
    IQueryable<RecipeStep> RecipeSteps { get; }
    IQueryable<Recipe> Recipes { get; }
    IQueryable<ShoppingList> ShoppingLists { get; }
    IQueryable<RecommendationComment> RecommendationComments { get; }
    IQueryable<Recommendation> Recommendations { get; }
    IQueryable<UserLessonProgress> UserLessonProgress { get; }
    IQueryable<UserLoginEvent> UserLoginEvents { get; }
    IQueryable<UserRoleAuditEvent> UserRoleAuditEvents { get; }
    IQueryable<User> Users { get; }
    IQueryable<WaistEntry> WaistEntries { get; }
    IQueryable<WeightEntry> WeightEntries { get; }

    IQueryable<AiUsage> PrivacyExportAiUsage { get; }
    IQueryable<BillingCancellation> PrivacyExportBillingCancellation { get; }
    IQueryable<BillingPayment> PrivacyExportBillingPayment { get; }
    IQueryable<BillingSubscription> PrivacyExportBillingSubscription { get; }
    IQueryable<WaistEntry> PrivacyExportWaistEntry { get; }
    IQueryable<WeightEntry> PrivacyExportWeightEntry { get; }
    IQueryable<ContentReport> PrivacyExportContentReport { get; }
    IQueryable<CycleProfile> PrivacyExportCycleProfile { get; }
    IQueryable<ExerciseEntry> PrivacyExportExerciseEntry { get; }
    IQueryable<FastingCheckIn> PrivacyExportFastingCheckIn { get; }
    IQueryable<FastingOccurrence> PrivacyExportFastingOccurrence { get; }
    IQueryable<FastingPlan> PrivacyExportFastingPlan { get; }
    IQueryable<FastingSession> PrivacyExportFastingSession { get; }
    IQueryable<FavoriteMeal> PrivacyExportFavoriteMeal { get; }
    IQueryable<FavoriteProduct> PrivacyExportFavoriteProduct { get; }
    IQueryable<FavoriteRecipe> PrivacyExportFavoriteRecipe { get; }
    IQueryable<UserAchievement> PrivacyExportUserAchievement { get; }
    IQueryable<HydrationEntry> PrivacyExportHydrationEntry { get; }
    IQueryable<UserLoginEvent> PrivacyExportUserLoginEvent { get; }
    IQueryable<UserRefreshTokenSession> PrivacyExportUserRefreshTokenSession { get; }
    IQueryable<ImageAsset> PrivacyExportImageAsset { get; }
    IQueryable<UserLessonProgress> PrivacyExportUserLessonProgress { get; }
    IQueryable<ShoppingList> PrivacyExportShoppingList { get; }
    IQueryable<Meal> PrivacyExportMeal { get; }
    IQueryable<Notification> PrivacyExportNotification { get; }
    IQueryable<WebPushSubscription> PrivacyExportWebPushSubscription { get; }
    IQueryable<Product> PrivacyExportProduct { get; }
    IQueryable<RecentItem> PrivacyExportRecentItem { get; }
    IQueryable<RecipeComment> PrivacyExportRecipeComment { get; }
    IQueryable<RecipeLike> PrivacyExportRecipeLike { get; }
    IQueryable<Recipe> PrivacyExportRecipe { get; }
    IQueryable<UserPrivacyConsentEvent> PrivacyExportUserPrivacyConsentEvent { get; }
    IQueryable<UserRole> PrivacyExportUserRole { get; }
    IQueryable<UserRoleAuditEvent> PrivacyExportUserRoleAuditEvent { get; }
    IQueryable<WaistGoal> PrivacyExportWaistGoal { get; }
    IQueryable<WeightGoal> PrivacyExportWeightGoal { get; }
    IQueryable<WearableConnection> PrivacyExportWearableConnection { get; }
    IQueryable<WearableSyncEntry> PrivacyExportWearableSyncEntry { get; }
    IQueryable<WeeklyGoal> PrivacyExportWeeklyGoal { get; }
    IQueryable<User> PrivacyExportUser { get; }
    IQueryable<UserPreferences> PrivacyExportUserPreferences { get; }
    IQueryable<UserNutritionProfile> PrivacyExportUserNutritionProfile { get; }
    IQueryable<MealItem> PrivacyExportMealItem { get; }
    IQueryable<MealAiSession> PrivacyExportMealAiSession { get; }
    IQueryable<MealAiItem> PrivacyExportMealAiItem { get; }
    IQueryable<RecipeStep> PrivacyExportRecipeStep { get; }
    IQueryable<RecipeIngredient> PrivacyExportRecipeIngredient { get; }
    IQueryable<MenstrualEpisode> PrivacyExportMenstrualEpisode { get; }
    IQueryable<FertilitySignal> PrivacyExportFertilitySignal { get; }
    IQueryable<CycleSymptomEntry> PrivacyExportCycleSymptomEntry { get; }
    IQueryable<CyclePredictionRevision> PrivacyExportCyclePredictionRevision { get; }
    IQueryable<CycleFactor> PrivacyExportCycleFactor { get; }
    IQueryable<CycleDayNote> PrivacyExportCycleDayNote { get; }
    IQueryable<CycleConsent> PrivacyExportCycleConsent { get; }
    IQueryable<BleedingEntry> PrivacyExportBleedingEntry { get; }
    IQueryable<MarketingAttributionEvent> PrivacyExportMarketingAttribution { get; }
    IQueryable<UserGoalRevision> PrivacyExportUserGoalRevision { get; }
}
