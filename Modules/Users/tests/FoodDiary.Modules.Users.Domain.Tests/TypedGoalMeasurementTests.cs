using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities.Tracking;
using FoodDiary.Modules.Users.Domain.Enums;

namespace FoodDiary.Modules.Users.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class TypedGoalMeasurementTests {
    private static readonly DateTime Now = new(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void DesiredDefaults_AreAbsentRatherThanZeroTargets() {
        DesiredWeightKg? weight = default;
        DesiredWaistCm? waist = default;
        Assert.Multiple(() => Assert.Null(weight), () => Assert.Null(waist));
        Assert.Throws<ArgumentOutOfRangeException>(() => DesiredWeightKg.Create(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => DesiredWaistCm.Create(0));
    }

    [Fact]
    public void TypedReplacement_PreservesMeasuredHistoryAndDesiredTarget() {
        var user = User.Create("typed-goals@example.com", "hash");
        WeightGoal previous = user.StartWeightGoalWithMeasurements(DesiredWeightKg.Create(70), MeasuredWeightKg.Create(80), Now);
        WeightGoal current = user.StartWeightGoalWithMeasurements(DesiredWeightKg.Create(68), MeasuredWeightKg.Create(79), Now.AddDays(1));
        Assert.Multiple(
            () => Assert.Equal(WeightGoalStatus.Replaced, previous.Status),
            () => Assert.Equal(79, previous.EndWeightKg),
            () => Assert.Equal(79, current.StartWeightKg),
            () => Assert.Equal(68, user.DesiredWeightKg),
            () => Assert.Equal(2, user.WeightGoals.Count));
        user.CancelWeightGoalWithMeasurement(Now.AddDays(2), MeasuredWeightKg.Create(78));
        Assert.Multiple(() => Assert.Equal(78, current.EndWeightKg), () => Assert.Null(user.DesiredWeightKg));
    }

    [Fact]
    public void Cancellation_RequiresMeasurementOnlyForActiveGoal() {
        var user = User.Create("typed-cancel@example.com", "hash");
        user.UpdateDesiredWaist(80);
        user.CancelWaistGoalWithMeasurement(default, endWaist: null);
        Assert.Null(user.DesiredWaistCm);
        WaistGoal goal = user.StartWaistGoalWithMeasurements(DesiredWaistCm.Create(80), MeasuredWaistCm.Create(90), Now);
        Assert.Throws<ArgumentNullException>(() => user.CancelWaistGoalWithMeasurement(Now, endWaist: null));
        Assert.Multiple(() => Assert.Equal(WaistGoalStatus.Active, goal.Status), () => Assert.Equal(80, user.DesiredWaistCm));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void MeasuredWeight_RejectsInvalidValues(double value) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => MeasuredWeightKg.Create(value));

}
