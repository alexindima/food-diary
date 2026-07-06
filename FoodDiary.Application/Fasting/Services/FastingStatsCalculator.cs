using FoodDiary.Application.Abstractions.Fasting.Models;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Fasting.Services;

internal static class FastingStatsCalculator {
    public static FastingStatsModel Create(
        IReadOnlyList<FastingOccurrenceReadModel> allOccurrences,
        IReadOnlyList<FastingOccurrenceAnalysis> allAnalyses,
        IReadOnlyList<FastingOccurrenceReadModel> last30Occurrences,
        IReadOnlyList<FastingOccurrenceAnalysis> last30Analyses,
        DateTime nowUtc) {
        var completedOccurrences = allOccurrences
            .Where(static occurrence => occurrence.Status == FastingOccurrenceStatus.Completed && occurrence.EndedAtUtc.HasValue)
            .ToList();
        var completedLast30Days = last30Occurrences
            .Where(static occurrence => occurrence.Status == FastingOccurrenceStatus.Completed && occurrence.EndedAtUtc.HasValue)
            .ToList();

        double averageDuration = completedLast30Days.Count > 0
            ? completedLast30Days.Average(occurrence => (occurrence.EndedAtUtc!.Value - occurrence.StartedAtUtc).TotalHours)
            : 0;
        double completionRate = last30Occurrences.Count > 0
            ? Math.Round(completedLast30Days.Count / (double)last30Occurrences.Count * 100, 1, MidpointRounding.ToEven) : 0;
        double checkInRate = last30Analyses.Count > 0
            ? Math.Round(last30Analyses.Count(static analysis => analysis.LatestCheckIn is not null) / (double)last30Analyses.Count * 100, 1, MidpointRounding.ToEven) : 0;
        DateTime? lastCheckInAtUtc = allAnalyses
            .Select(static analysis => analysis.LatestCheckIn?.CheckedInAtUtc)
            .Where(static checkedInAtUtc => checkedInAtUtc.HasValue)
            .Max();

        return new FastingStatsModel(
            completedOccurrences.Count,
            CalculateCurrentStreak(completedOccurrences, nowUtc.Date),
            Math.Round(averageDuration, 1, MidpointRounding.ToEven),
            completionRate,
            checkInRate,
            lastCheckInAtUtc,
            GetTopSymptom(last30Analyses));
    }

    private static int CalculateCurrentStreak(IReadOnlyList<FastingOccurrenceReadModel> completedOccurrences, DateTime todayUtcDate) {
        if (completedOccurrences.Count == 0) {
            return 0;
        }

        int streak = 0;
        DateTime expectedDate = todayUtcDate;

        foreach (FastingOccurrenceReadModel occurrence in completedOccurrences.OrderByDescending(static occurrence => occurrence.StartedAtUtc)) {
            DateTime occurrenceDate = occurrence.StartedAtUtc.Date;
            if (occurrenceDate == expectedDate || occurrenceDate == expectedDate.AddDays(-1)) {
                streak++;
                expectedDate = occurrenceDate.AddDays(-1);
            } else {
                break;
            }
        }

        return streak;
    }

    private static string? GetTopSymptom(IReadOnlyList<FastingOccurrenceAnalysis> analyses) {
        var symptomCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (string symptom in analyses
                     .SelectMany(static analysis => analysis.Timeline)
                     .SelectMany(static checkIn => checkIn.Symptoms)) {
            symptomCounts[symptom] = symptomCounts.TryGetValue(symptom, out int count) ? count + 1 : 1;
        }

        return FastingSymptomCatalog.PrioritizedSymptoms.FirstOrDefault(symptom => symptomCounts.GetValueOrDefault(symptom) >= 2);
    }
}
