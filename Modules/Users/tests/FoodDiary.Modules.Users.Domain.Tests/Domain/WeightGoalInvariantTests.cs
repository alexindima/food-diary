using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class WeightGoalInvariantTests {
    [Fact]
    public void Start_CreatesActiveGoalWithItsOwnBaseline() {
        DateTime startedAtUtc = new(2026, 8, 5, 12, 0, 0, DateTimeKind.Utc);

        var goal = WeightGoal.Start(UserId.New(), 72, 81.5, startedAtUtc);

        Assert.Multiple(
            () => Assert.Equal(72, goal.TargetWeightKg),
            () => Assert.Equal(81.5, goal.StartWeightKg),
            () => Assert.Equal(startedAtUtc, goal.StartedAtUtc),
            () => Assert.Equal(WeightGoalStatus.Active, goal.Status),
            () => Assert.Null(goal.EndedAtUtc));
    }

    [Fact]
    public void Replace_ClosesActiveGoalWithoutLosingItsBaseline() {
        DateTime startedAtUtc = new(2026, 8, 5, 12, 0, 0, DateTimeKind.Utc);
        DateTime endedAtUtc = startedAtUtc.AddDays(7);
        var goal = WeightGoal.Start(UserId.New(), 72, 81.5, startedAtUtc);

        goal.Replace(endedAtUtc, 78);

        Assert.Multiple(
            () => Assert.Equal(WeightGoalStatus.Replaced, goal.Status),
            () => Assert.Equal(endedAtUtc, goal.EndedAtUtc),
            () => Assert.Equal(78, goal.EndWeightKg),
            () => Assert.Equal(81.5, goal.StartWeightKg),
            () => Assert.Equal(startedAtUtc, goal.StartedAtUtc));
    }

    [Fact]
    public void Cancel_ClosesActiveGoal() {
        DateTime startedAtUtc = new(2026, 8, 5, 12, 0, 0, DateTimeKind.Utc);
        var goal = WeightGoal.Start(UserId.New(), 72, 81.5, startedAtUtc);

        goal.Cancel(startedAtUtc.AddDays(1), 80);

        Assert.Multiple(
            () => Assert.Equal(WeightGoalStatus.Cancelled, goal.Status),
            () => Assert.Equal(80, goal.EndWeightKg));
    }

    [Fact]
    public void End_WhenGoalIsAlreadyClosed_Throws() {
        DateTime startedAtUtc = new(2026, 8, 5, 12, 0, 0, DateTimeKind.Utc);
        var goal = WeightGoal.Start(UserId.New(), 72, 81.5, startedAtUtc);
        goal.Cancel(startedAtUtc.AddDays(1), 80);

        Assert.Throws<InvalidOperationException>(() => goal.Replace(startedAtUtc.AddDays(2), 79));
    }

    [Fact]
    public void Start_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            WeightGoal.Start(new UserId(Guid.Empty), 72, 81.5, DateTime.UtcNow));
    }

    [Fact]
    public void Cancel_WhenEndedBeforeStart_Throws() {
        DateTime startedAtUtc = new(2026, 8, 5, 12, 0, 0, DateTimeKind.Utc);
        var goal = WeightGoal.Start(UserId.New(), 72, 81.5, startedAtUtc);

        Assert.Throws<ArgumentOutOfRangeException>(() => goal.Cancel(startedAtUtc.AddTicks(-1), 80));
    }
}
