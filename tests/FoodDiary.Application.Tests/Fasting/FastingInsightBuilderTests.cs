using FoodDiary.Application.Abstractions.Fasting.Models;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Fasting.Services;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Tests.Fasting;

[ExcludeFromCodeCoverage]
public sealed class FastingInsightBuilderTests {
    private static readonly DateTime FixedNow = new(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void BuildAlerts_WithLateCurrentAndNoCheckIn_ReturnsLatePrompt() {
        var user = User.Create("insight-late@example.com", "hash");
        var plan = FastingPlan.CreateExtended(user.Id, FastingProtocol.F36_0, 36, FixedNow.AddDays(-1));
        var occurrence = FastingOccurrence.Create(
            plan.Id,
            user.Id,
            FastingOccurrenceKind.FastDay,
            FixedNow.AddHours(-21),
            sequenceNumber: 1,
            targetHours: 36);

        IReadOnlyList<FastingMessageModel> alerts = FastingInsightBuilder.BuildAlerts(
            ToReadModel(occurrence, plan),
            latestCheckIn: null,
            FixedNow);

        FastingMessageModel alert = Assert.Single(alerts);
        Assert.Equal("late", alert.Id);
        Assert.Equal("warning", alert.Tone);
    }

    [Fact]
    public void BuildAlerts_WithRiskyCurrentCheckIn_ReturnsCurrentWarningAndRiskyPrompt() {
        var user = User.Create("insight-risky@example.com", "hash");
        var plan = FastingPlan.CreateExtended(user.Id, FastingProtocol.F36_0, 36, FixedNow.AddDays(-1));
        var occurrence = FastingOccurrence.Create(
            plan.Id,
            user.Id,
            FastingOccurrenceKind.FastDay,
            FixedNow.AddHours(-18),
            sequenceNumber: 1,
            targetHours: 36);
        var checkIn = new FastingCheckInSnapshot(FixedNow, HungerLevel: 4, EnergyLevel: 1, MoodLevel: 2, Symptoms: ["dizziness"], Notes: null);

        IReadOnlyList<FastingMessageModel> alerts = FastingInsightBuilder.BuildAlerts(
            ToReadModel(occurrence, plan),
            checkIn,
            FixedNow);

        Assert.Collection(
            alerts,
            first => Assert.Equal("current-warning", first.Id),
            second => Assert.Equal("risky", second.Id));
    }

    [Fact]
    public void BuildInsights_WithRecurringPrioritizedSymptom_ReturnsSymptomInsight() {
        IReadOnlyList<FastingOccurrenceAnalysis> analyses = [
            CreateAnalysis(targetHours: 16, energy: 4, mood: 4, symptoms: ["headache"]),
            CreateAnalysis(targetHours: 18, energy: 4, mood: 4, symptoms: ["headache"]),
        ];

        IReadOnlyList<FastingMessageModel> insights = FastingInsightBuilder.BuildInsights(analyses);

        FastingMessageModel insight = Assert.Single(insights);
        Assert.Equal("symptom-headache", insight.Id);
        Assert.Equal("neutral", insight.Tone);
        Assert.Equal("FASTING.CHECK_IN.SYMPTOMS.HEADACHE", insight.BodyParams?["symptom"]);
    }

    [Fact]
    public void BuildInsights_WithBetterShortFastsAndStrongCheckIns_ReturnsShorterAndPositiveInsights() {
        IReadOnlyList<FastingOccurrenceAnalysis> analyses = [
            CreateAnalysis(targetHours: 16, energy: 5, mood: 5, symptoms: []),
            CreateAnalysis(targetHours: 18, energy: 5, mood: 4, symptoms: []),
            CreateAnalysis(targetHours: 20, energy: 4, mood: 5, symptoms: []),
            CreateAnalysis(targetHours: 30, energy: 2, mood: 2, symptoms: []),
            CreateAnalysis(targetHours: 36, energy: 2, mood: 3, symptoms: []),
        ];

        IReadOnlyList<FastingMessageModel> insights = FastingInsightBuilder.BuildInsights(analyses);

        Assert.Contains(insights, insight => string.Equals(insight.Id, "shorter-fasts", StringComparison.Ordinal));
        Assert.Contains(insights, insight => string.Equals(insight.Id, "positive-tolerance", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildInsights_WhenShorterFastsAreNotBetter_DoesNotReturnShorterInsight() {
        IReadOnlyList<FastingOccurrenceAnalysis> analyses = [
            CreateAnalysis(targetHours: 16, energy: 3, mood: 3, symptoms: []),
            CreateAnalysis(targetHours: 18, energy: 3, mood: 3, symptoms: []),
            CreateAnalysis(targetHours: 30, energy: 4, mood: 4, symptoms: []),
            CreateAnalysis(targetHours: 36, energy: 4, mood: 4, symptoms: []),
        ];

        IReadOnlyList<FastingMessageModel> insights = FastingInsightBuilder.BuildInsights(analyses);

        Assert.DoesNotContain(insights, insight => string.Equals(insight.Id, "shorter-fasts", StringComparison.Ordinal));
    }

    private static FastingOccurrenceAnalysis CreateAnalysis(
        int targetHours,
        int energy,
        int mood,
        IReadOnlyList<string> symptoms) {
        var user = User.Create($"insight-{Guid.NewGuid():N}@example.com", "hash");
        var plan = FastingPlan.CreateExtended(user.Id, FastingProtocol.F36_0, targetHours, FixedNow.AddDays(-1));
        var occurrence = FastingOccurrence.Create(
            plan.Id,
            user.Id,
            FastingOccurrenceKind.FastDay,
            FixedNow.AddHours(-targetHours),
            sequenceNumber: 1,
            targetHours);
        var checkIn = new FastingCheckInSnapshot(FixedNow, HungerLevel: 3, EnergyLevel: energy, MoodLevel: mood, Symptoms: symptoms, Notes: null);
        IReadOnlyList<FastingCheckInSnapshot> timeline = [checkIn];

        return new FastingOccurrenceAnalysis(ToReadModel(occurrence, plan), timeline, checkIn);
    }

    private static FastingOccurrenceReadModel ToReadModel(FastingOccurrence occurrence, FastingPlan plan) =>
        new(
            occurrence.Id,
            occurrence.PlanId,
            new FastingPlanReadModel(
                plan.Id,
                plan.UserId,
                plan.Type,
                plan.Status,
                plan.Protocol,
                plan.Title,
                plan.StartedAtUtc,
                plan.StoppedAtUtc,
                plan.IntermittentFastHours,
                plan.IntermittentEatingWindowHours,
                plan.ExtendedTargetHours,
                plan.CyclicFastDays,
                plan.CyclicEatDays,
                plan.CyclicEatDayFastHours,
                plan.CyclicEatDayEatingWindowHours,
                plan.CyclicAnchorDateUtc,
                plan.CyclicNextPhaseDateUtc),
            occurrence.UserId,
            occurrence.Kind,
            occurrence.Status,
            occurrence.SequenceNumber,
            occurrence.ScheduledForUtc,
            occurrence.StartedAtUtc,
            occurrence.EndedAtUtc,
            occurrence.InitialTargetHours,
            occurrence.AddedTargetHours,
            occurrence.Notes,
            occurrence.CheckInAtUtc,
            occurrence.HungerLevel,
            occurrence.EnergyLevel,
            occurrence.MoodLevel,
            occurrence.Symptoms,
            occurrence.CheckInNotes);
}
