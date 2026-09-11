using FoodDiary.Modules.Billing.Infrastructure.Persistence;
using FoodDiary.Modules.Fasting.Infrastructure.Persistence;
using FoodDiary.Modules.ContentReports.Infrastructure.Persistence;
using FoodDiary.Modules.Lessons.Infrastructure.Persistence;
using FoodDiary.Modules.Notifications.Infrastructure.Model;
using FoodDiary.Modules.BodyMetrics.Infrastructure.Persistence;
using FoodDiary.Modules.Cycles.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Hydration.Infrastructure.Persistence;
using FoodDiary.Modules.WeeklyGoals.Infrastructure.Persistence;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ScalarPersistenceBoundaryTests {
    [Theory]
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
