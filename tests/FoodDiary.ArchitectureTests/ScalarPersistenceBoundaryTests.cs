using FoodDiary.Modules.Exercises.PersistenceModel;
using FoodDiary.Modules.Dietologist.PersistenceModel;
using FoodDiary.Modules.Identity.PersistenceModel;
using FoodDiary.Modules.Cycles.PersistenceModel;
using FoodDiary.Modules.ContentReports.PersistenceModel;
using FoodDiary.Modules.Billing.PersistenceModel;
using FoodDiary.Modules.Fasting.PersistenceModel;
using FoodDiary.Modules.Lessons.Infrastructure.Persistence;
using FoodDiary.Modules.Notifications.Infrastructure.Model;
using FoodDiary.Modules.BodyMetrics.PersistenceModel;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Ai.PersistenceModel;
using FoodDiary.Modules.Admin.PersistenceModel;
using FoodDiary.Modules.Hydration.Infrastructure.Persistence;
using FoodDiary.Modules.WeeklyGoals.Infrastructure.Persistence;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ScalarPersistenceBoundaryTests {
    [Theory]
    [InlineData(typeof(RecipesPersistenceModelRegistration), "Recipes")]
    [InlineData(typeof(MealsPersistenceModelRegistration), "Meals")]
    [InlineData(typeof(ProductsPersistenceModelRegistration), "Products")]
    [InlineData(typeof(FoodDiary.Modules.Favorites.PersistenceModel.FavoritesPersistenceModelRegistration), "Favorites")]
    [InlineData(typeof(FoodDiary.Modules.MealPlanning.Infrastructure.Model.MealPlanningPersistenceModelRegistration), "MealPlanning")]
    [InlineData(typeof(FoodDiary.Infrastructure.UsersPersistenceModelRegistration), "Users")]
    [InlineData(typeof(DietologistPersistenceModelRegistration), "Dietologist")]
    [InlineData(typeof(RecipeCommunityPersistenceModelRegistration), "RecipeCommunity")]
    [InlineData(typeof(AdminPersistenceModelRegistration), "Admin")]
    [InlineData(typeof(IdentityPersistenceModelRegistration), "Identity")]
    [InlineData(typeof(FastingPersistenceModelRegistration), "Fasting")]
    [InlineData(typeof(BillingPersistenceModelRegistration), "Billing")]
    [InlineData(typeof(ContentReportsPersistenceModelRegistration), "ContentReports")]
    [InlineData(typeof(LessonsPersistenceModelRegistration), "Lessons")]
    [InlineData(typeof(GamificationPersistenceModelRegistration), "Gamification")]
    [InlineData(typeof(NotificationsPersistenceModelRegistration), "Notifications")]
    [InlineData(typeof(AiPersistenceModelRegistration), "Ai")]
    [InlineData(typeof(ImagesPersistenceModelBuilderExtensions), "Images")]
    [InlineData(typeof(CyclesPersistenceModelRegistration), "Cycles")]
    [InlineData(typeof(BodyMetricsPersistenceModelRegistration), "BodyMetrics")]
    [InlineData(typeof(WearablesPersistenceModelRegistration), "Wearables")]
    [InlineData(typeof(HydrationPersistenceModelRegistration), "Hydration")]
    [InlineData(typeof(RecentItemsPersistenceModelRegistration), "RecentItems")]
    [InlineData(typeof(ExercisesPersistenceModelRegistration), "Exercises")]
    [InlineData(typeof(WeeklyGoalsPersistenceModelRegistration), "WeeklyGoals")]
    public void PersistenceModel_DoesNotReferenceForeignDomainAssemblies(Type registration, string owner) {
        Assert.DoesNotContain(registration.Assembly.GetReferencedAssemblies(), reference =>
            reference.Name!.StartsWith("FoodDiary.Modules.", StringComparison.Ordinal)
            && reference.Name.EndsWith(".Domain", StringComparison.Ordinal)
            && !string.Equals(reference.Name, $"FoodDiary.Modules.{owner}.Domain", StringComparison.Ordinal));
    }
}
