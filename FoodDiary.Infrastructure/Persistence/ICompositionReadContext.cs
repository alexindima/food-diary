using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Hydration.Domain.Entities.Tracking;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.Lessons.Domain.Entities.Content;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteMeals;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteProducts;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteRecipes;
using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Users;
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
    IQueryable<RecipeStep> RecipeSteps { get; }
    IQueryable<Recipe> Recipes { get; }
    IQueryable<RecommendationComment> RecommendationComments { get; }
    IQueryable<Recommendation> Recommendations { get; }
    IQueryable<UserLessonProgress> UserLessonProgress { get; }
    IQueryable<UserLoginEvent> UserLoginEvents { get; }
    IQueryable<UserRoleAuditEvent> UserRoleAuditEvents { get; }
    IQueryable<User> Users { get; }
    IQueryable<WaistEntry> WaistEntries { get; }
    IQueryable<WeightEntry> WeightEntries { get; }
}
