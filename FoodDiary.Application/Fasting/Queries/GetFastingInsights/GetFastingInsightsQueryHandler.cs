using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Application.Fasting.Queries.GetFastingInsights;

public sealed class GetFastingInsightsQueryHandler(
    IFastingOccurrenceRepository fastingOccurrenceRepository,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetFastingInsightsQuery, Result<FastingInsightsModel>> {
    private static readonly HashSet<string> RiskySymptoms = ["dizziness", "weakness"];
    private static readonly string[] PrioritizedSymptoms = ["dizziness", "weakness", "headache", "irritability", "cravings"];
    private const int AnalysisDays = 90;

    public async Task<Result<FastingInsightsModel>> Handle(
        GetFastingInsightsQuery query,
        CancellationToken cancellationToken) {
        var userId = new UserId(query.UserId!.Value);
        var now = dateTimeProvider.UtcNow;
        var current = await fastingOccurrenceRepository.GetCurrentAsync(userId, cancellationToken: cancellationToken);
        var history = await fastingOccurrenceRepository.GetByUserAsync(
            userId,
            from: now.AddDays(-AnalysisDays),
            to: now,
            cancellationToken: cancellationToken);
        var historyWithCheckIns = history.Where(static occurrence => HasCheckIn(occurrence)).ToList();

        var alerts = BuildAlerts(current, now);
        var insights = BuildInsights(historyWithCheckIns);
        return Result.Success(new FastingInsightsModel(alerts, insights));
    }

    private static IReadOnlyList<FastingMessageModel> BuildInsights(IReadOnlyList<FastingOccurrence> history) {
        var insights = new List<FastingMessageModel>();

        var shorterTolerance = BuildShorterToleranceInsight(history);
        if (shorterTolerance is not null) {
            insights.Add(shorterTolerance);
        }

        var recurringSymptom = BuildRecurringSymptomInsight(history);
        if (recurringSymptom is not null) {
            insights.Add(recurringSymptom);
        }

        var positiveTolerance = BuildPositiveToleranceInsight(history);
        if (positiveTolerance is not null) {
            insights.Add(positiveTolerance);
        }

        return insights.Take(3).ToList();
    }

    private static IReadOnlyList<FastingMessageModel> BuildAlerts(FastingOccurrence? current, DateTime now) {
        var alerts = new List<FastingMessageModel>();

        var currentWarning = BuildCurrentWarning(current);
        if (currentWarning is not null) {
            alerts.Add(currentWarning);
        }

        if (current is null || current.EndedAtUtc.HasValue) {
            return alerts;
        }

        if (HasRiskyCurrentCheckIn(current)) {
            alerts.Add(new FastingMessageModel(
                "risky",
                "FASTING.PROMPTS.RISKY_TITLE",
                "FASTING.PROMPTS.RISKY_BODY",
                "warning"));
            return alerts;
        }

        if (current.CheckInAtUtc.HasValue) {
            return alerts;
        }

        var elapsedHours = (now - current.StartedAtUtc).TotalHours;
        if (elapsedHours >= 20) {
            alerts.Add(new FastingMessageModel(
                "late",
                "FASTING.PROMPTS.LATE_TITLE",
                "FASTING.PROMPTS.LATE_BODY",
                "warning"));
        } else if (elapsedHours >= 12) {
            alerts.Add(new FastingMessageModel(
                "mid",
                "FASTING.PROMPTS.MID_TITLE",
                "FASTING.PROMPTS.MID_BODY",
                "neutral"));
        }

        return alerts;
    }

    private static FastingMessageModel? BuildCurrentWarning(FastingOccurrence? current) {
        if (current is null || !HasRiskyCurrentCheckIn(current)) {
            return null;
        }

        return new FastingMessageModel(
            "current-warning",
            "FASTING.INSIGHTS.CURRENT_WARNING_TITLE",
            "FASTING.INSIGHTS.CURRENT_WARNING_BODY",
            "warning");
    }

    private static FastingMessageModel? BuildShorterToleranceInsight(IReadOnlyList<FastingOccurrence> history) {
        var shorter = history.Where(static occurrence => occurrence.TargetHours.HasValue && occurrence.TargetHours.Value < 24).ToList();
        var longer = history.Where(static occurrence => occurrence.TargetHours.HasValue && occurrence.TargetHours.Value >= 24).ToList();

        if (shorter.Count < 2 || longer.Count < 2) {
            return null;
        }

        var shorterScore = GetAverageToleranceScore(shorter);
        var longerScore = GetAverageToleranceScore(longer);
        if (shorterScore - longerScore < 1) {
            return null;
        }

        return new FastingMessageModel(
            "shorter-fasts",
            "FASTING.INSIGHTS.SHORTER_FASTS_TITLE",
            "FASTING.INSIGHTS.SHORTER_FASTS_BODY",
            "neutral");
    }

    private static FastingMessageModel? BuildRecurringSymptomInsight(IReadOnlyList<FastingOccurrence> history) {
        var symptomCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var occurrence in history) {
            foreach (var symptom in GetSymptoms(occurrence)) {
                symptomCounts[symptom] = symptomCounts.TryGetValue(symptom, out var count) ? count + 1 : 1;
            }
        }

        var recurring = PrioritizedSymptoms.FirstOrDefault(symptom => symptomCounts.GetValueOrDefault(symptom) >= 2);
        if (string.IsNullOrWhiteSpace(recurring)) {
            return null;
        }

        return new FastingMessageModel(
            $"symptom-{recurring}",
            "FASTING.INSIGHTS.RECURRING_SYMPTOM_TITLE",
            "FASTING.INSIGHTS.RECURRING_SYMPTOM_BODY",
            RiskySymptoms.Contains(recurring) ? "warning" : "neutral",
            new Dictionary<string, string> {
                ["symptom"] = $"FASTING.CHECK_IN.SYMPTOMS.{recurring.ToUpperInvariant()}",
            });
    }

    private static FastingMessageModel? BuildPositiveToleranceInsight(IReadOnlyList<FastingOccurrence> history) {
        var strongCheckIns = history.Where(occurrence =>
            (occurrence.EnergyLevel ?? 0) >= 4 &&
            (occurrence.MoodLevel ?? 0) >= 4 &&
            !GetSymptoms(occurrence).Any(RiskySymptoms.Contains)).ToList();

        if (strongCheckIns.Count < 3) {
            return null;
        }

        return new FastingMessageModel(
            "positive-tolerance",
            "FASTING.INSIGHTS.POSITIVE_TITLE",
            "FASTING.INSIGHTS.POSITIVE_BODY",
            "positive");
    }

    private static bool HasCheckIn(FastingOccurrence occurrence) => occurrence.CheckInAtUtc.HasValue;

    private static bool HasRiskyCurrentCheckIn(FastingOccurrence occurrence) {
        var lowEnergy = (occurrence.EnergyLevel ?? 5) <= 2;
        var lowMood = (occurrence.MoodLevel ?? 5) <= 2;
        var hasRiskySymptom = GetSymptoms(occurrence).Any(RiskySymptoms.Contains);

        return HasCheckIn(occurrence) && hasRiskySymptom && (lowEnergy || lowMood);
    }

    private static double GetAverageToleranceScore(IReadOnlyList<FastingOccurrence> occurrences) {
        var total = occurrences.Sum(occurrence => (occurrence.EnergyLevel ?? 0) + (occurrence.MoodLevel ?? 0));
        return total / (occurrences.Count * 2d);
    }

    private static IReadOnlyList<string> GetSymptoms(FastingOccurrence occurrence) =>
        string.IsNullOrWhiteSpace(occurrence.Symptoms)
            ? []
            : occurrence.Symptoms.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
