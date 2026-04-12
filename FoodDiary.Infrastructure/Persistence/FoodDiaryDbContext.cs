using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recents;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Usda;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.Entities.Wearables;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public class FoodDiaryDbContext(DbContextOptions<FoodDiaryDbContext> options) : DbContext(options) {
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<ImageAsset> ImageAssets => Set<ImageAsset>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<RecentItem> RecentItems => Set<RecentItem>();
    public DbSet<Meal> Meals => Set<Meal>();
    public DbSet<MealItem> MealItems => Set<MealItem>();
    public DbSet<MealAiSession> MealAiSessions => Set<MealAiSession>();
    public DbSet<MealAiItem> MealAiItems => Set<MealAiItem>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();
    public DbSet<ShoppingListItem> ShoppingListItems => Set<ShoppingListItem>();
    public DbSet<WeightEntry> WeightEntries => Set<WeightEntry>();
    public DbSet<WaistEntry> WaistEntries => Set<WaistEntry>();
    public DbSet<Cycle> Cycles => Set<Cycle>();
    public DbSet<CycleDay> CycleDays => Set<CycleDay>();
    public DbSet<HydrationEntry> HydrationEntries => Set<HydrationEntry>();
    public DbSet<DailyAdvice> DailyAdvices => Set<DailyAdvice>();
    public DbSet<AiUsage> AiUsages => Set<AiUsage>();
    public DbSet<AiPromptTemplate> AiPromptTemplates => Set<AiPromptTemplate>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<DietologistInvitation> DietologistInvitations => Set<DietologistInvitation>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<WebPushSubscription> WebPushSubscriptions => Set<WebPushSubscription>();
    public DbSet<FastingPlan> FastingPlans => Set<FastingPlan>();
    public DbSet<FastingOccurrence> FastingOccurrences => Set<FastingOccurrence>();
    public DbSet<FastingCheckIn> FastingCheckIns => Set<FastingCheckIn>();
    public DbSet<FastingSession> FastingSessions => Set<FastingSession>();
    public DbSet<FastingTelemetryEvent> FastingTelemetryEvents => Set<FastingTelemetryEvent>();
    public DbSet<FavoriteMeal> FavoriteMeals => Set<FavoriteMeal>();
    public DbSet<ExerciseEntry> ExerciseEntries => Set<ExerciseEntry>();
    public DbSet<NutritionLesson> NutritionLessons => Set<NutritionLesson>();
    public DbSet<UserLessonProgress> UserLessonProgress => Set<UserLessonProgress>();
    public DbSet<MealPlan> MealPlans => Set<MealPlan>();
    public DbSet<MealPlanDay> MealPlanDays => Set<MealPlanDay>();
    public DbSet<MealPlanMeal> MealPlanMeals => Set<MealPlanMeal>();
    public DbSet<UsdaFood> UsdaFoods => Set<UsdaFood>();
    public DbSet<UsdaNutrient> UsdaNutrients => Set<UsdaNutrient>();
    public DbSet<UsdaFoodNutrient> UsdaFoodNutrients => Set<UsdaFoodNutrient>();
    public DbSet<UsdaFoodPortion> UsdaFoodPortions => Set<UsdaFoodPortion>();
    public DbSet<DailyReferenceValue> DailyReferenceValues => Set<DailyReferenceValue>();
    public DbSet<RecipeComment> RecipeComments => Set<RecipeComment>();
    public DbSet<RecipeLike> RecipeLikes => Set<RecipeLike>();
    public DbSet<ContentReport> ContentReports => Set<ContentReport>();
    public DbSet<WearableConnection> WearableConnections => Set<WearableConnection>();
    public DbSet<WearableSyncEntry> WearableSyncEntries => Set<WearableSyncEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FoodDiaryDbContext).Assembly);
    }
}
