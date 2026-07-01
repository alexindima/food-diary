using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Application.Fasting.Services;

internal static class FastingInsightBuilder {
    public static IReadOnlyList<FastingMessageModel> BuildInsights(IReadOnlyList<FastingOccurrenceAnalysis> analyses) {
        var history = analyses
            .Where(static analysis => analysis.LatestCheckIn is not null)
            .ToList();
        var insights = new List<FastingMessageModel>();

        FastingMessageModel? shorterTolerance = BuildShorterToleranceInsight(history);
        if (shorterTolerance is not null) {
            insights.Add(shorterTolerance);
        }

        FastingMessageModel? recurringSymptom = BuildRecurringSymptomInsight(history);
        if (recurringSymptom is not null) {
            insights.Add(recurringSymptom);
        }

        FastingMessageModel? positiveTolerance = BuildPositiveToleranceInsight(history);
        if (positiveTolerance is not null) {
            insights.Add(positiveTolerance);
        }

        return insights.Take(3).ToList();
    }

    public static IReadOnlyList<FastingMessageModel> BuildAlerts(
        FastingOccurrence? current,
        FastingCheckInSnapshot? latestCheckIn,
        DateTime nowUtc) {
        var alerts = new List<FastingMessageModel>();

        FastingMessageModel? currentWarning = BuildCurrentWarning(current, latestCheckIn);
        if (currentWarning is not null) {
            alerts.Add(currentWarning);
        }

        if (current?.EndedAtUtc.HasValue != false) {
            return alerts;
        }

        if (HasRiskyCurrentCheckIn(current, latestCheckIn)) {
            alerts.Add(new FastingMessageModel(
                "risky",
                "FASTING.PROMPTS.RISKY_TITLE",
                "FASTING.PROMPTS.RISKY_BODY",
                "warning"));
            return alerts;
        }

        if (latestCheckIn is not null) {
            return alerts;
        }

        switch ((nowUtc - current.StartedAtUtc).TotalHours) {
            case >= 20:
                alerts.Add(new FastingMessageModel(
                    "late",
                    "FASTING.PROMPTS.LATE_TITLE",
                    "FASTING.PROMPTS.LATE_BODY",
                    "warning"));
                break;
            case >= 12:
                alerts.Add(new FastingMessageModel(
                    "mid",
                    "FASTING.PROMPTS.MID_TITLE",
                    "FASTING.PROMPTS.MID_BODY",
                    "neutral"));
                break;
        }

        return alerts;
    }

    private static FastingMessageModel? BuildCurrentWarning(FastingOccurrence? current, FastingCheckInSnapshot? latestCheckIn) {
        if (current is null || !HasRiskyCurrentCheckIn(current, latestCheckIn)) {
            return null;
        }

        return new FastingMessageModel(
            "current-warning",
            "FASTING.INSIGHTS.CURRENT_WARNING_TITLE",
            "FASTING.INSIGHTS.CURRENT_WARNING_BODY",
            "warning");
    }

    private static FastingMessageModel? BuildShorterToleranceInsight(IReadOnlyList<FastingOccurrenceAnalysis> history) {
        var shorter = history
            .Where(static analysis => analysis.Occurrence.TargetHours < 24 && analysis.LatestCheckIn is not null)
            .ToList();
        var longer = history
            .Where(static analysis => analysis.Occurrence.TargetHours >= 24 && analysis.LatestCheckIn is not null)
            .ToList();

        if (shorter.Count < 2 || longer.Count < 2) {
            return null;
        }

        double shorterScore = shorter.Average(static analysis => GetToleranceScore(analysis.LatestCheckIn!));
        double longerScore = longer.Average(static analysis => GetToleranceScore(analysis.LatestCheckIn!));
        if (shorterScore - longerScore < 1) {
            return null;
        }

        return new FastingMessageModel(
            "shorter-fasts",
            "FASTING.INSIGHTS.SHORTER_FASTS_TITLE",
            "FASTING.INSIGHTS.SHORTER_FASTS_BODY",
            "neutral");
    }

    private static FastingMessageModel? BuildRecurringSymptomInsight(IReadOnlyList<FastingOccurrenceAnalysis> history) {
        var symptomCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (string symptom in history
                     .SelectMany(static analysis => analysis.Timeline)
                     .SelectMany(static checkIn => checkIn.Symptoms)) {
            symptomCounts[symptom] = symptomCounts.TryGetValue(symptom, out int count) ? count + 1 : 1;
        }

        string? recurring = FastingSymptomCatalog.PrioritizedSymptoms.FirstOrDefault(symptom => symptomCounts.GetValueOrDefault(symptom) >= 2);
        if (string.IsNullOrWhiteSpace(recurring)) {
            return null;
        }

        return new FastingMessageModel(
            $"symptom-{recurring}",
            "FASTING.INSIGHTS.RECURRING_SYMPTOM_TITLE",
            "FASTING.INSIGHTS.RECURRING_SYMPTOM_BODY",
            FastingSymptomCatalog.RiskySymptoms.Contains(recurring) ? "warning" : "neutral",
            new Dictionary<string, string>(StringComparer.Ordinal) {
                ["symptom"] = $"FASTING.CHECK_IN.SYMPTOMS.{recurring.ToUpperInvariant()}",
            });
    }

    private static FastingMessageModel? BuildPositiveToleranceInsight(IReadOnlyList<FastingOccurrenceAnalysis> history) {
        var strongCheckIns = history
            .SelectMany(static analysis => analysis.Timeline)
            .Where(checkIn =>
                checkIn is { EnergyLevel: >= 4, MoodLevel: >= 4 } &&
                !checkIn.Symptoms.Any(FastingSymptomCatalog.RiskySymptoms.Contains))
            .ToList();

        if (strongCheckIns.Count < 3) {
            return null;
        }

        return new FastingMessageModel(
            "positive-tolerance",
            "FASTING.INSIGHTS.POSITIVE_TITLE",
            "FASTING.INSIGHTS.POSITIVE_BODY",
            "positive");
    }

    private static bool HasRiskyCurrentCheckIn(FastingOccurrence occurrence, FastingCheckInSnapshot? latestCheckIn) {
        if (latestCheckIn is null) {
            return false;
        }

        bool lowEnergy = latestCheckIn.EnergyLevel <= 2;
        bool lowMood = latestCheckIn.MoodLevel <= 2;
        bool hasRiskySymptom = latestCheckIn.Symptoms.Any(FastingSymptomCatalog.RiskySymptoms.Contains);

        return occurrence.EndedAtUtc is null && hasRiskySymptom && (lowEnergy || lowMood);
    }

    private static double GetToleranceScore(FastingCheckInSnapshot checkIn) => checkIn.EnergyLevel + checkIn.MoodLevel;
}
