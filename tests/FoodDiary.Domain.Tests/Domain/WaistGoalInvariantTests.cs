using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class WaistGoalInvariantTests {
    [Fact]
    public void Start_CreatesActiveGoalWithItsOwnBaseline() {
        DateTime startedAtUtc = new(2026, 8, 6, 12, 0, 0, DateTimeKind.Utc);
        var goal = WaistGoal.Start(UserId.New(), 75, 88.5, startedAtUtc);

        Assert.Multiple(
            () => Assert.Equal(75, goal.TargetWaist),
            () => Assert.Equal(88.5, goal.StartWaist),
            () => Assert.Equal(startedAtUtc, goal.StartedAtUtc),
            () => Assert.Equal(WaistGoalStatus.Active, goal.Status),
            () => Assert.Null(goal.EndedAtUtc));
    }

    [Fact]
    public void Replace_ClosesActiveGoalWithoutLosingItsBaseline() {
        DateTime startedAtUtc = new(2026, 8, 6, 12, 0, 0, DateTimeKind.Utc);
        DateTime endedAtUtc = startedAtUtc.AddDays(7);
        var goal = WaistGoal.Start(UserId.New(), 75, 88.5, startedAtUtc);
        goal.Replace(endedAtUtc, 84);

        Assert.Multiple(
            () => Assert.Equal(WaistGoalStatus.Replaced, goal.Status),
            () => Assert.Equal(endedAtUtc, goal.EndedAtUtc),
            () => Assert.Equal(84, goal.EndWaist),
            () => Assert.Equal(88.5, goal.StartWaist));
    }

    [Fact]
    public void Cancel_ClosesActiveGoal() {
        DateTime startedAtUtc = new(2026, 8, 6, 12, 0, 0, DateTimeKind.Utc);
        var goal = WaistGoal.Start(UserId.New(), 75, 88.5, startedAtUtc);
        goal.Cancel(startedAtUtc.AddDays(1), 87);

        Assert.Multiple(
            () => Assert.Equal(WaistGoalStatus.Cancelled, goal.Status),
            () => Assert.Equal(87, goal.EndWaist));
    }

    [Fact]
    public void End_WhenGoalIsAlreadyClosed_Throws() {
        DateTime startedAtUtc = new(2026, 8, 6, 12, 0, 0, DateTimeKind.Utc);
        var goal = WaistGoal.Start(UserId.New(), 75, 88.5, startedAtUtc);
        goal.Cancel(startedAtUtc.AddDays(1), 87);

        Assert.Throws<InvalidOperationException>(() => goal.Replace(startedAtUtc.AddDays(2), 86));
    }
}
