using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class UserGoalAtomicityTests {
    private static readonly DateTime Now = new(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void GoalReplacement_WhenNewGoalIsInvalid_IsAtomic() {
        var user = User.Create("goals@example.com", "hash");
        WeightGoal weightGoal = user.StartWeightGoal(70, 80, Now);
        WaistGoal waistGoal = user.StartWaistGoal(80, 90, Now);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.StartWeightGoal(double.NaN, 79, Now.AddDays(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.StartWaistGoal(double.PositiveInfinity, 89, Now.AddDays(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.StartWeightGoal(69, 79, DateTime.SpecifyKind(Now.AddDays(1), DateTimeKind.Unspecified)));

        Assert.Multiple(
            () => Assert.Equal(WeightGoalStatus.Active, weightGoal.Status),
            () => Assert.Null(weightGoal.EndedAtUtc),
            () => Assert.Single(user.WeightGoals),
            () => Assert.Equal(70, user.DesiredWeightKg),
            () => Assert.Equal(WaistGoalStatus.Active, waistGoal.Status),
            () => Assert.Null(waistGoal.EndedAtUtc),
            () => Assert.Single(user.WaistGoals),
            () => Assert.Equal(80, user.DesiredWaistCm));
    }
}
