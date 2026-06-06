using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

[ExcludeFromCodeCoverage]
public class FastingPlanInvariantTests {
    [Fact]
    public void CreateIntermittent_WithValidValues_Succeeds() {
        DateTime startedAtUtc = DateTime.UtcNow;

        var plan = FastingPlan.CreateIntermittent(
            UserId.New(),
            FastingProtocol.CustomIntermittent,
            fastHours: 17,
            eatingWindowHours: 7,
            startedAtUtc: startedAtUtc,
            title: "  My plan  ");

        Assert.Equal(FastingPlanType.Intermittent, plan.Type);
        Assert.Equal(FastingPlanStatus.Active, plan.Status);
        Assert.Equal(FastingProtocol.CustomIntermittent, plan.Protocol);
        Assert.Equal(17, plan.IntermittentFastHours);
        Assert.Equal(7, plan.IntermittentEatingWindowHours);
        Assert.Equal("My plan", plan.Title);
        Assert.Equal(startedAtUtc, plan.StartedAtUtc);
    }

    [Fact]
    public void CreateIntermittent_WithHoursThatDoNotAddTo24_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FastingPlan.CreateIntermittent(
                UserId.New(),
                FastingProtocol.CustomIntermittent,
                fastHours: 17,
                eatingWindowHours: 8,
                startedAtUtc: DateTime.UtcNow));
    }

    [Fact]
    public void CreateExtended_WithIntermittentProtocol_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FastingPlan.CreateExtended(
                UserId.New(),
                FastingProtocol.F16_8,
                targetHours: 48,
                startedAtUtc: DateTime.UtcNow));
    }

    [Fact]
    public void CreateCyclic_WithValidValues_Succeeds() {
        var anchorDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

        var plan = FastingPlan.CreateCyclic(
            UserId.New(),
            fastDays: 1,
            eatDays: 3,
            eatDayFastHours: 16,
            eatDayEatingWindowHours: 8,
            anchorDateUtc: anchorDateUtc,
            startedAtUtc: DateTime.UtcNow);

        Assert.Equal(FastingPlanType.Cyclic, plan.Type);
        Assert.Equal(1, plan.CyclicFastDays);
        Assert.Equal(3, plan.CyclicEatDays);
        Assert.Equal(16, plan.CyclicEatDayFastHours);
        Assert.Equal(8, plan.CyclicEatDayEatingWindowHours);
        Assert.Equal(anchorDateUtc, plan.CyclicAnchorDateUtc);
        Assert.Equal(anchorDateUtc, plan.CyclicNextPhaseDateUtc);
    }

    [Fact]
    public void Stop_SetsStoppedState() {
        var plan = FastingPlan.CreateExtended(
            UserId.New(),
            FastingProtocol.F72_0,
            targetHours: 72,
            startedAtUtc: DateTime.UtcNow);
        DateTime stoppedAtUtc = DateTime.UtcNow.AddHours(4);

        plan.Stop(stoppedAtUtc);

        Assert.Equal(FastingPlanStatus.Stopped, plan.Status);
        Assert.Equal(stoppedAtUtc, plan.StoppedAtUtc);
    }

    [Fact]
    public void CreateIntermittent_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            FastingPlan.CreateIntermittent(UserId.Empty, FastingProtocol.F16_8, 16, 8, DateTime.UtcNow));
    }

    [Fact]
    public void CreateExtended_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            FastingPlan.CreateExtended(UserId.Empty, FastingProtocol.Custom, 24, DateTime.UtcNow));
    }

    [Fact]
    public void CreateCyclic_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            FastingPlan.CreateCyclic(UserId.Empty, 1, 1, 16, 8, DateTime.UtcNow, DateTime.UtcNow));
    }

    [Fact]
    public void CreateIntermittent_WithExtendedProtocol_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FastingPlan.CreateIntermittent(UserId.New(), FastingProtocol.F72_0, 16, 8, DateTime.UtcNow));
    }

    [Theory]
    [InlineData(0, 24)]
    [InlineData(24, 24)]
    [InlineData(24, 0)]
    [InlineData(1, 24)]
    [InlineData(24, 1)]
    public void CreateIntermittent_WithInvalidHours_Throws(int fastHours, int eatingWindowHours) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FastingPlan.CreateIntermittent(UserId.New(), FastingProtocol.CustomIntermittent, fastHours, eatingWindowHours, DateTime.UtcNow));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(169)]
    public void CreateExtended_WithInvalidTarget_Throws(int targetHours) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FastingPlan.CreateExtended(UserId.New(), FastingProtocol.Custom, targetHours, DateTime.UtcNow));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(31, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 31)]
    public void CreateCyclic_WithInvalidDays_Throws(int fastDays, int eatDays) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FastingPlan.CreateCyclic(UserId.New(), fastDays, eatDays, 16, 8, DateTime.UtcNow, DateTime.UtcNow));
    }

    [Fact]
    public void CreateCyclic_WithUnspecifiedAnchorDate_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FastingPlan.CreateCyclic(
                UserId.New(),
                fastDays: 1,
                eatDays: 1,
                eatDayFastHours: 16,
                eatDayEatingWindowHours: 8,
                anchorDateUtc: new DateTime(2026, 3, 27),
                startedAtUtc: DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithUnspecifiedTimestamp_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FastingPlan.CreateExtended(UserId.New(), FastingProtocol.Custom, 24, new DateTime(2026, 3, 27)));
    }

    [Fact]
    public void Create_WithTooLongTitle_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FastingPlan.CreateExtended(UserId.New(), FastingProtocol.Custom, 24, DateTime.UtcNow, new string('t', 121)));
    }

    [Fact]
    public void Create_WithWhitespaceTitle_NormalizesToNull() {
        var plan = FastingPlan.CreateExtended(UserId.New(), FastingProtocol.Custom, 24, DateTime.UtcNow, "   ");

        Assert.Null(plan.Title);
    }

    [Fact]
    public void PauseResumeStopAndRename_AreIdempotentWhenStateDoesNotChange() {
        var plan = FastingPlan.CreateExtended(UserId.New(), FastingProtocol.Custom, 24, DateTime.UtcNow, "Plan");

        plan.Pause();
        Assert.Equal(FastingPlanStatus.Paused, plan.Status);
        DateTime? modifiedAfterPause = plan.ModifiedOnUtc;
        plan.Pause();
        Assert.Equal(modifiedAfterPause, plan.ModifiedOnUtc);

        plan.Resume();
        Assert.Equal(FastingPlanStatus.Active, plan.Status);
        plan.Rename("  Plan  ");
        Assert.Equal("Plan", plan.Title);

        DateTime stoppedAtUtc = DateTime.UtcNow.AddHours(1);
        plan.Stop(stoppedAtUtc);
        DateTime? modifiedAfterStop = plan.ModifiedOnUtc;
        plan.Stop(stoppedAtUtc.AddHours(1));
        Assert.Equal(stoppedAtUtc, plan.StoppedAtUtc);
        Assert.Equal(modifiedAfterStop, plan.ModifiedOnUtc);
    }

    [Fact]
    public void Resume_WhenAlreadyActive_DoesNotSetModifiedOnUtc() {
        var plan = FastingPlan.CreateExtended(UserId.New(), FastingProtocol.Custom, 24, DateTime.UtcNow);

        plan.Resume();

        Assert.Equal(FastingPlanStatus.Active, plan.Status);
        Assert.Null(plan.ModifiedOnUtc);
    }

    [Fact]
    public void Rename_WithDifferentTitle_UpdatesTitle() {
        var plan = FastingPlan.CreateExtended(UserId.New(), FastingProtocol.Custom, 24, DateTime.UtcNow, "Plan");

        plan.Rename("  New plan  ");

        Assert.Equal("New plan", plan.Title);
        Assert.NotNull(plan.ModifiedOnUtc);
    }

    [Fact]
    public void ScheduleNextCyclicPhase_WithCyclicPlan_UpdatesDate() {
        var plan = FastingPlan.CreateCyclic(UserId.New(), 1, 3, 16, 8, DateTime.UtcNow, DateTime.UtcNow);
        DateTime nextDate = DateTime.UtcNow.AddDays(1);

        plan.ScheduleNextCyclicPhase(nextDate);

        Assert.Equal(DateTime.SpecifyKind(nextDate.ToUniversalTime().Date, DateTimeKind.Utc), plan.CyclicNextPhaseDateUtc);
        Assert.NotNull(plan.ModifiedOnUtc);
    }

    [Fact]
    public void ScheduleNextCyclicPhase_WithSameDate_DoesNotSetModifiedOnUtc() {
        var anchorDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var plan = FastingPlan.CreateCyclic(UserId.New(), 1, 3, 16, 8, anchorDateUtc, DateTime.UtcNow);

        plan.ScheduleNextCyclicPhase(anchorDateUtc);

        Assert.Equal(anchorDateUtc, plan.CyclicNextPhaseDateUtc);
        Assert.Null(plan.ModifiedOnUtc);
    }

    [Fact]
    public void ScheduleNextCyclicPhase_WithNonCyclicPlan_Throws() {
        var plan = FastingPlan.CreateExtended(UserId.New(), FastingProtocol.Custom, 24, DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => plan.ScheduleNextCyclicPhase(DateTime.UtcNow.AddDays(1)));
    }
}
