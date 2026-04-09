using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

public class FastingPlanInvariantTests {
    [Fact]
    public void CreateIntermittent_WithValidValues_Succeeds() {
        var startedAtUtc = DateTime.UtcNow;

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
        var stoppedAtUtc = DateTime.UtcNow.AddHours(4);

        plan.Stop(stoppedAtUtc);

        Assert.Equal(FastingPlanStatus.Stopped, plan.Status);
        Assert.Equal(stoppedAtUtc, plan.StoppedAtUtc);
    }
}
