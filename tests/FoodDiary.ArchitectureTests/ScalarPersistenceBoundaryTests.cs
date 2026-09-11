using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Hydration.Infrastructure.Persistence;
using FoodDiary.Modules.WeeklyGoals.Infrastructure.Persistence;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ScalarPersistenceBoundaryTests {
    [Theory]
    [InlineData(typeof(AiPersistenceModelRegistration), "Ai")]
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
