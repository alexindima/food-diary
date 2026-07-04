using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Fasting;

public partial class FastingFeatureTests {
    [Fact]
    public void FastingMappings_ToModel_WithSession_MapsProtocolPlanTypeAndOccurrenceKind() {
        var session = FastingSession.Create(
            UserId.New(),
            FastingProtocol.F16_8,
            plannedDurationHours: 16,
            FixedNow,
            notes: "morning fast");

        FastingSessionModel model = session.ToModel();

        Assert.Equal("F16_8", model.Protocol);
        Assert.Equal("Intermittent", model.PlanType);
        Assert.Equal("FastingWindow", model.OccurrenceKind);
        Assert.Equal("Active", model.Status);
        Assert.Equal("morning fast", model.Notes);
        Assert.Empty(model.CheckIns);
    }

    [Fact]
    public void FastingMappings_ToModel_WithCompletedExtendedSession_MapsFastDayAndCompletion() {
        var session = FastingSession.Create(
            UserId.New(),
            FastingProtocol.F36_0,
            plannedDurationHours: 36,
            FixedNow.AddHours(-40),
            notes: null);
        session.End(FixedNow);

        FastingSessionModel model = session.ToModel();

        Assert.Equal("Extended", model.PlanType);
        Assert.Equal("FastDay", model.OccurrenceKind);
        Assert.True(model.IsCompleted);
        Assert.Equal("Completed", model.Status);
        Assert.Equal(FixedNow, model.EndedAtUtc);
    }

    [Theory]
    [InlineData(FastingProtocol.F18_6)]
    [InlineData(FastingProtocol.F20_4)]
    [InlineData(FastingProtocol.CustomIntermittent)]
    public void FastingMappings_ToModel_WithIntermittentSessionProtocols_MapsIntermittentWindow(FastingProtocol protocol) {
        var session = FastingSession.Create(
            UserId.New(),
            protocol,
            plannedDurationHours: 18,
            FixedNow,
            notes: null);

        FastingSessionModel model = session.ToModel();

        Assert.Equal("Intermittent", model.PlanType);
        Assert.Equal("FastingWindow", model.OccurrenceKind);
    }

    [Fact]
    public void FastingMappings_ToModel_WithOccurrenceOverload_UsesOccurrenceDefaults() {
        var occurrence = FastingOccurrence.Create(
            FastingPlanId.New(),
            UserId.New(),
            FastingOccurrenceKind.FastDay,
            FixedNow.AddHours(-5),
            sequenceNumber: 1,
            targetHours: null,
            notes: "custom");

        FastingSessionModel model = occurrence.ToModel();

        Assert.Equal("Custom", model.Protocol);
        Assert.Equal("Extended", model.PlanType);
        Assert.Equal("FastDay", model.OccurrenceKind);
        Assert.Equal(16, model.InitialPlannedDurationHours);
        Assert.Equal("custom", model.Notes);
    }

    [Fact]
    public void FastingMappings_ToModel_WithCyclicEatingWindow_HasNoPhaseProgress() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateCyclic(
            userId,
            fastDays: 2,
            eatDays: 1,
            eatDayFastHours: 16,
            eatDayEatingWindowHours: 8,
            anchorDateUtc: FixedNow,
            startedAtUtc: FixedNow);
        var occurrence = FastingOccurrence.Create(
            plan.Id,
            userId,
            FastingOccurrenceKind.EatingWindow,
            FixedNow,
            sequenceNumber: 3,
            targetHours: 8,
            notes: null);

        FastingSessionModel model = occurrence.ToModel(plan);

        Assert.Equal("Cyclic", model.PlanType);
        Assert.Equal("EatingWindow", model.OccurrenceKind);
        Assert.Null(model.CyclicPhaseDayNumber);
        Assert.Null(model.CyclicPhaseDayTotal);
    }

    [Fact]
    public void FastingMappings_ToModel_WithOccurrence_UsesLatestCheckInAndDistinctSymptoms() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateExtended(userId, FastingProtocol.F36_0, 36, FixedNow.AddHours(-1));
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow.AddHours(-1), 1, 36);
        occurrence.UpdateCheckIn(2, 3, 4, ["tired"], "old check-in", FixedNow.AddMinutes(-30));
        var olderCheckIn = FastingCheckIn.Create(
            occurrence.Id,
            userId,
            hungerLevel: 1,
            energyLevel: 2,
            moodLevel: 3,
            symptoms: ["old"],
            notes: "older",
            checkedInAtUtc: FixedNow.AddMinutes(-20));
        var latestCheckIn = FastingCheckIn.Create(
            occurrence.Id,
            userId,
            hungerLevel: 4,
            energyLevel: 5,
            moodLevel: 3,
            symptoms: ["headache", "Headache", "focused"],
            notes: "latest",
            checkedInAtUtc: FixedNow.AddMinutes(-10));

        FastingSessionModel model = occurrence.ToModel(plan, [olderCheckIn, latestCheckIn]);

        Assert.Equal("Extended", model.PlanType);
        Assert.Equal("FastDay", model.OccurrenceKind);
        Assert.Equal(36, model.InitialPlannedDurationHours);
        Assert.Equal(FixedNow.AddMinutes(-10), model.CheckInAtUtc);
        Assert.Equal(4, model.HungerLevel);
        Assert.Equal(5, model.EnergyLevel);
        Assert.Equal("latest", model.CheckInNotes);
        Assert.Equal(["headache", "focused"], model.Symptoms);
        Assert.Collection(
            model.CheckIns,
            checkIn => Assert.Equal(latestCheckIn.Id.Value, checkIn.Id),
            checkIn => Assert.Equal(olderCheckIn.Id.Value, checkIn.Id));
    }

    [Fact]
    public void FastingMappings_ToModel_WithCyclicEatDay_UsesEatingWindowDefaultsAndPhaseProgress() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateCyclic(
            userId,
            fastDays: 2,
            eatDays: 3,
            eatDayFastHours: 18,
            eatDayEatingWindowHours: 6,
            anchorDateUtc: FixedNow.AddDays(-4),
            startedAtUtc: FixedNow.AddDays(-4));
        var occurrence = FastingOccurrence.Create(
            plan.Id,
            userId,
            FastingOccurrenceKind.EatDay,
            FixedNow,
            sequenceNumber: 4);

        FastingSessionModel model = occurrence.ToModel(plan);

        Assert.Equal("Cyclic", model.PlanType);
        Assert.Equal("EatDay", model.OccurrenceKind);
        Assert.Equal(6, model.InitialPlannedDurationHours);
        Assert.Equal(6, model.PlannedDurationHours);
        Assert.Equal(2, model.CyclicPhaseDayNumber);
        Assert.Equal(3, model.CyclicPhaseDayTotal);
        Assert.False(model.IsCompleted);
    }

    [Fact]
    public void FastingMappings_ToModel_WithOccurrenceWithoutPlan_UsesCustomDefaultsAndOccurrenceCheckIn() {
        var userId = UserId.New();
        var occurrence = FastingOccurrence.Create(
            FastingPlanId.New(),
            userId,
            FastingOccurrenceKind.FastDay,
            FixedNow.AddHours(-3),
            sequenceNumber: 1);
        occurrence.UpdateCheckIn(3, 4, 5, ["weakness", "Weakness"], "fallback", FixedNow.AddHours(-1));

        FastingSessionModel model = occurrence.ToModel(plan: null);

        Assert.Equal("Custom", model.Protocol);
        Assert.Equal("Extended", model.PlanType);
        Assert.Equal(16, model.InitialPlannedDurationHours);
        Assert.Equal(FixedNow.AddHours(-1), model.CheckInAtUtc);
        Assert.Equal(["weakness"], model.Symptoms);
        Assert.Equal("fallback", model.CheckInNotes);
    }
}
