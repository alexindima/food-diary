using FoodDiary.Application.Fasting.Models;

namespace FoodDiary.Application.Fasting.Services;

internal static class FastingHistoricalInsightBuilder {
    public static IReadOnlyList<FastingMessageModel> Build(IReadOnlyList<FastingOccurrenceAnalysis> analyses) {
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

    private static double GetToleranceScore(FastingCheckInSnapshot checkIn) => checkIn.EnergyLevel + checkIn.MoodLevel;
}
