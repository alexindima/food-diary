using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Fasting.Services;
using FoodDiary.Application.Abstractions.Fasting.Models;
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
        Assert.Null(model.CheckInAtUtc);
        Assert.Empty(model.Symptoms);
    }

    [Fact]
    public void FastingMappings_ToModel_WithCheckInDomainEntity_MapsSymptomsAndNotes() {
        var occurrenceId = FastingOccurrenceId.New();
        var checkIn = FastingCheckIn.Create(
            occurrenceId,
            UserId.New(),
            hungerLevel: 2,
            energyLevel: 4,
            moodLevel: 3,
            symptoms: ["headache", "Headache", "focused"],
            notes: "steady",
            checkedInAtUtc: FixedNow);

        FastingCheckInModel model = checkIn.ToModel();

        Assert.Equal(checkIn.Id.Value, model.Id);
        Assert.Equal(FixedNow, model.CheckedInAtUtc);
        Assert.Equal(2, model.HungerLevel);
        Assert.Equal(4, model.EnergyLevel);
        Assert.Equal(3, model.MoodLevel);
        Assert.Equal(["headache", "focused"], model.Symptoms);
        Assert.Equal("steady", model.Notes);
    }

    [Fact]
    public void FastingCheckInTimelineBuilder_WithCheckIns_ReturnsNewestFirst() {
        var occurrence = FastingOccurrence.Create(
            FastingPlanId.New(),
            UserId.New(),
            FastingOccurrenceKind.FastDay,
            FixedNow.AddHours(-4),
            sequenceNumber: 1);
        var older = FastingCheckIn.Create(occurrence.Id, occurrence.UserId, 2, 3, 4, ["tired"], "older", FixedNow.AddHours(-2));
        var latest = FastingCheckIn.Create(occurrence.Id, occurrence.UserId, 4, 5, 3, ["focused", "Focused"], "latest", FixedNow.AddHours(-1));

        IReadOnlyList<FastingCheckInSnapshot> timeline = FastingCheckInTimelineBuilder.Build(occurrence, [older, latest]);

        Assert.Collection(
            timeline,
            snapshot => {
                Assert.Equal(FixedNow.AddHours(-1), snapshot.CheckedInAtUtc);
                Assert.Equal(["focused"], snapshot.Symptoms);
                Assert.Equal("latest", snapshot.Notes);
            },
            snapshot => Assert.Equal(FixedNow.AddHours(-2), snapshot.CheckedInAtUtc));
    }

    [Fact]
    public void FastingCheckInTimelineBuilder_WithoutCheckIns_UsesOccurrenceFallback() {
        var occurrence = FastingOccurrence.Create(
            FastingPlanId.New(),
            UserId.New(),
            FastingOccurrenceKind.FastDay,
            FixedNow.AddHours(-4),
            sequenceNumber: 1);
        occurrence.UpdateCheckIn(3, 4, 5, ["weakness", "Weakness"], "fallback", FixedNow.AddHours(-1));

        FastingCheckInSnapshot snapshot = Assert.Single(FastingCheckInTimelineBuilder.Build(occurrence, checkIns: null));

        Assert.Equal(FixedNow.AddHours(-1), snapshot.CheckedInAtUtc);
        Assert.Equal(3, snapshot.HungerLevel);
        Assert.Equal(4, snapshot.EnergyLevel);
        Assert.Equal(5, snapshot.MoodLevel);
        Assert.Equal(["weakness"], snapshot.Symptoms);
        Assert.Equal("fallback", snapshot.Notes);
    }

    [Fact]
    public void FastingCheckInTimelineBuilder_WithoutAnyCheckIn_ReturnsEmptyTimeline() {
        var occurrence = FastingOccurrence.Create(
            FastingPlanId.New(),
            UserId.New(),
            FastingOccurrenceKind.FastDay,
            FixedNow.AddHours(-4),
            sequenceNumber: 1);

        Assert.Empty(FastingCheckInTimelineBuilder.Build(occurrence, checkIns: null));
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

    [Fact]
    public void FastingMappings_ToModel_WithReadModelOccurrence_UsesLatestCheckInAndCyclicProgress() {
        var userId = UserId.New();
        FastingPlanReadModel plan = CreateCyclicPlanReadModel(userId);
        FastingOccurrenceReadModel occurrence = CreateCompletedEatDayReadModel(userId, plan);
        var checkIn = new FastingCheckInReadModel(
            FastingCheckInId.New(),
            occurrence.Id,
            FixedNow.AddHours(-1),
            HungerLevel: 4,
            EnergyLevel: 5,
            MoodLevel: 3,
            Symptoms: "calm,Calm,focused",
            Notes: "latest");
        var olderCheckIn = new FastingCheckInReadModel(
            FastingCheckInId.New(),
            occurrence.Id,
            FixedNow.AddHours(-3),
            HungerLevel: 2,
            EnergyLevel: 2,
            MoodLevel: 2,
            Symptoms: "tired",
            Notes: "older");

        FastingSessionModel model = occurrence.ToModel(plan, [olderCheckIn, checkIn]);

        Assert.Equal("Custom", model.Protocol);
        Assert.Equal("Cyclic", model.PlanType);
        Assert.True(model.IsCompleted);
        Assert.Equal(7, model.PlannedDurationHours);
        Assert.Equal(2, model.CyclicPhaseDayNumber);
        Assert.Equal(3, model.CyclicPhaseDayTotal);
        Assert.Equal(FixedNow.AddHours(-1), model.CheckInAtUtc);
        Assert.Equal(["calm", "focused"], model.Symptoms);
        Assert.Equal("latest", model.CheckInNotes);
        Assert.Equal(2, model.CheckIns.Count);
        Assert.Equal("latest", model.CheckIns[0].Notes);
        Assert.Equal("older", model.CheckIns[1].Notes);
    }

    [Fact]
    public void FastingMappings_ToModel_WithReadModelOccurrenceOverload_UsesCustomDefaults() {
        var userId = UserId.New();
        var occurrence = new FastingOccurrenceReadModel(
            FastingOccurrenceId.New(),
            FastingPlanId.New(),
            Plan: null,
            userId,
            FastingOccurrenceKind.FastDay,
            FastingOccurrenceStatus.Active,
            SequenceNumber: 1,
            ScheduledForUtc: null,
            StartedAtUtc: FixedNow.AddHours(-3),
            EndedAtUtc: null,
            InitialTargetHours: null,
            AddedTargetHours: 0,
            Notes: "read model",
            CheckInAtUtc: null,
            HungerLevel: null,
            EnergyLevel: null,
            MoodLevel: null,
            Symptoms: null,
            CheckInNotes: null);

        FastingSessionModel model = occurrence.ToModel();

        Assert.Equal("Custom", model.Protocol);
        Assert.Equal("Extended", model.PlanType);
        Assert.Equal(16, model.InitialPlannedDurationHours);
        Assert.Null(model.CyclicPhaseDayNumber);
        Assert.Empty(model.CheckIns);
    }

    [Fact]
    public void FastingMappings_ToModel_WithReadModelCyclicFastDay_UsesFastDayPhaseProgress() {
        var userId = UserId.New();
        FastingPlanReadModel plan = CreateCyclicPlanReadModel(userId);
        var occurrence = new FastingOccurrenceReadModel(
            FastingOccurrenceId.New(),
            plan.Id,
            plan,
            userId,
            FastingOccurrenceKind.FastDay,
            FastingOccurrenceStatus.Active,
            SequenceNumber: 2,
            ScheduledForUtc: null,
            StartedAtUtc: FixedNow.AddHours(-18),
            EndedAtUtc: null,
            InitialTargetHours: null,
            AddedTargetHours: 0,
            Notes: null,
            CheckInAtUtc: null,
            HungerLevel: null,
            EnergyLevel: null,
            MoodLevel: null,
            Symptoms: null,
            CheckInNotes: null);

        FastingSessionModel model = occurrence.ToModel(plan);

        Assert.Equal(18, model.InitialPlannedDurationHours);
        Assert.Equal(2, model.CyclicPhaseDayNumber);
        Assert.Equal(2, model.CyclicPhaseDayTotal);
    }

    private static FastingPlanReadModel CreateCyclicPlanReadModel(UserId userId) =>
        new(
            FastingPlanId.New(),
            userId,
            FastingPlanType.Cyclic,
            FastingPlanStatus.Active,
            Protocol: null,
            Title: "Cycle",
            FixedNow.AddDays(-5),
            StoppedAtUtc: null,
            IntermittentFastHours: null,
            IntermittentEatingWindowHours: null,
            ExtendedTargetHours: null,
            CyclicFastDays: 2,
            CyclicEatDays: 3,
            CyclicEatDayFastHours: 18,
            CyclicEatDayEatingWindowHours: 6,
            CyclicAnchorDateUtc: FixedNow.AddDays(-5),
            CyclicNextPhaseDateUtc: FixedNow);

    private static FastingOccurrenceReadModel CreateCompletedEatDayReadModel(UserId userId, FastingPlanReadModel plan) =>
        new(
            FastingOccurrenceId.New(),
            plan.Id,
            plan,
            userId,
            FastingOccurrenceKind.EatDay,
            FastingOccurrenceStatus.Completed,
            SequenceNumber: 4,
            ScheduledForUtc: null,
            StartedAtUtc: FixedNow.AddHours(-6),
            EndedAtUtc: FixedNow,
            InitialTargetHours: null,
            AddedTargetHours: 1,
            Notes: "done",
            CheckInAtUtc: FixedNow.AddHours(-2),
            HungerLevel: 2,
            EnergyLevel: 2,
            MoodLevel: 2,
            Symptoms: "old",
            CheckInNotes: "old note");
}
