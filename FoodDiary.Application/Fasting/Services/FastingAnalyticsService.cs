using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Services;

public sealed class FastingAnalyticsService(
    IFastingOccurrenceRepository fastingOccurrenceRepository,
    IFastingCheckInRepository fastingCheckInRepository)
    : IFastingAnalyticsService {
    private static readonly HashSet<string> RiskySymptoms = ["dizziness", "weakness"];
    private static readonly string[] PrioritizedSymptoms = ["dizziness", "weakness", "headache", "irritability", "cravings"];
    private const int AnalysisDays = 90;

    public (DateTime FromUtc, DateTime ToUtc) GetDefaultHistoryWindow(DateTime nowUtc) {
        var currentMonthStart = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return (currentMonthStart.AddMonths(-1), currentMonthStart.AddMonths(2).AddTicks(-1));
    }

    public async Task<FastingStatsModel> GetStatsAsync(UserId userId, DateTime nowUtc, CancellationToken cancellationToken) {
        var allOccurrences = await fastingOccurrenceRepository.GetByUserAsync(userId, cancellationToken: cancellationToken);
        var allAnalyses = await BuildAnalysesAsync(allOccurrences, cancellationToken);
        var completedOccurrences = allOccurrences
            .Where(static occurrence => occurrence.Status == FastingOccurrenceStatus.Completed && occurrence.EndedAtUtc.HasValue)
            .ToList();

        var last30Occurrences = await fastingOccurrenceRepository.GetByUserAsync(
            userId,
            from: nowUtc.AddDays(-30),
            to: nowUtc,
            cancellationToken: cancellationToken);
        var last30Analyses = await BuildAnalysesAsync(last30Occurrences, cancellationToken);
        var completedLast30Days = last30Occurrences
            .Where(static occurrence => occurrence.Status == FastingOccurrenceStatus.Completed && occurrence.EndedAtUtc.HasValue)
            .ToList();

        var averageDuration = completedLast30Days.Count > 0
            ? completedLast30Days.Average(occurrence => (occurrence.EndedAtUtc!.Value - occurrence.StartedAtUtc).TotalHours)
            : 0;
        var completionRate = last30Occurrences.Count > 0
            ? Math.Round(completedLast30Days.Count / (double)last30Occurrences.Count * 100, 1)
            : 0;
        var checkInRate = last30Analyses.Count > 0
            ? Math.Round(last30Analyses.Count(static analysis => analysis.LatestCheckIn is not null) / (double)last30Analyses.Count * 100, 1)
            : 0;
        var lastCheckInAtUtc = allAnalyses
            .Select(static analysis => analysis.LatestCheckIn?.CheckedInAtUtc)
            .Where(static checkedInAtUtc => checkedInAtUtc.HasValue)
            .Max();

        return new FastingStatsModel(
            completedOccurrences.Count,
            CalculateCurrentStreak(completedOccurrences, nowUtc.Date),
            Math.Round(averageDuration, 1),
            completionRate,
            checkInRate,
            lastCheckInAtUtc,
            GetTopSymptom(last30Analyses));
    }

    public async Task<FastingInsightsModel> GetInsightsAsync(
        UserId userId,
        DateTime nowUtc,
        FastingOccurrence? current,
        CancellationToken cancellationToken) {
        var history = await fastingOccurrenceRepository.GetByUserAsync(
            userId,
            from: nowUtc.AddDays(-AnalysisDays),
            to: nowUtc,
            cancellationToken: cancellationToken);
        var analyses = await BuildAnalysesAsync(history, cancellationToken);
        var currentLatestCheckIn = current is null
            ? null
            : (await BuildAnalysesAsync([current], cancellationToken)).FirstOrDefault()?.LatestCheckIn;

        return new FastingInsightsModel(
            BuildAlerts(current, currentLatestCheckIn, nowUtc),
            BuildInsights(analyses.Where(static analysis => analysis.LatestCheckIn is not null).ToList()));
    }

    public async Task<PagedResponse<FastingSessionModel>> GetHistoryAsync(
        UserId userId,
        int page,
        int limit,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken) {
        var (occurrences, totalItems) = await fastingOccurrenceRepository.GetPagedByUserAsync(
            userId,
            from: fromUtc,
            to: toUtc,
            page: page,
            limit: limit,
            cancellationToken: cancellationToken);
        var checkInsByOccurrence = await GetCheckInsByOccurrenceAsync(occurrences.Select(static occurrence => occurrence.Id).ToArray(), cancellationToken);
        var models = occurrences
            .Select(occurrence => occurrence.ToModel(
                occurrence.Plan,
                checkInsByOccurrence.GetValueOrDefault(occurrence.Id)))
            .ToList();
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)limit);

        return new PagedResponse<FastingSessionModel>(models, page, limit, totalPages, totalItems);
    }

    private async Task<IReadOnlyList<FastingOccurrenceAnalysis>> BuildAnalysesAsync(
        IReadOnlyList<FastingOccurrence> occurrences,
        CancellationToken cancellationToken) {
        if (occurrences.Count == 0) {
            return [];
        }

        var checkInsByOccurrence = await GetCheckInsByOccurrenceAsync(occurrences.Select(static occurrence => occurrence.Id).ToArray(), cancellationToken);
        return occurrences
            .Select(occurrence => {
                var timeline = BuildTimeline(occurrence, checkInsByOccurrence.GetValueOrDefault(occurrence.Id));
                return new FastingOccurrenceAnalysis(occurrence, timeline, timeline.FirstOrDefault());
            })
            .ToList();
    }

    private async Task<IReadOnlyDictionary<FastingOccurrenceId, IReadOnlyList<FastingCheckIn>>> GetCheckInsByOccurrenceAsync(
        IReadOnlyCollection<FastingOccurrenceId> occurrenceIds,
        CancellationToken cancellationToken) {
        if (occurrenceIds.Count == 0) {
            return new Dictionary<FastingOccurrenceId, IReadOnlyList<FastingCheckIn>>();
        }

        return (await fastingCheckInRepository.GetByOccurrenceIdsAsync(occurrenceIds, cancellationToken))
            .GroupBy(static checkIn => checkIn.OccurrenceId)
            .ToDictionary(static group => group.Key, static group => (IReadOnlyList<FastingCheckIn>)group.ToList());
    }

    private static IReadOnlyList<FastingCheckInSnapshot> BuildTimeline(
        FastingOccurrence occurrence,
        IReadOnlyList<FastingCheckIn>? checkIns) {
        if (checkIns is { Count: > 0 }) {
            return checkIns
                .OrderByDescending(static checkIn => checkIn.CheckedInAtUtc)
                .Select(static checkIn => new FastingCheckInSnapshot(
                    checkIn.CheckedInAtUtc,
                    checkIn.HungerLevel,
                    checkIn.EnergyLevel,
                    checkIn.MoodLevel,
                    ParseSymptoms(checkIn.Symptoms),
                    checkIn.Notes))
                .ToList();
        }

        if (!occurrence.CheckInAtUtc.HasValue) {
            return [];
        }

        return [
            new FastingCheckInSnapshot(
                occurrence.CheckInAtUtc.Value,
                occurrence.HungerLevel ?? 0,
                occurrence.EnergyLevel ?? 0,
                occurrence.MoodLevel ?? 0,
                ParseSymptoms(occurrence.Symptoms),
                occurrence.CheckInNotes)
        ];
    }

    private static IReadOnlyList<FastingMessageModel> BuildInsights(IReadOnlyList<FastingOccurrenceAnalysis> history) {
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

    private static IReadOnlyList<FastingMessageModel> BuildAlerts(
        FastingOccurrence? current,
        FastingCheckInSnapshot? latestCheckIn,
        DateTime nowUtc) {
        var alerts = new List<FastingMessageModel>();

        var currentWarning = BuildCurrentWarning(current, latestCheckIn);
        if (currentWarning is not null) {
            alerts.Add(currentWarning);
        }

        if (current is null || current.EndedAtUtc.HasValue) {
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

        var elapsedHours = (nowUtc - current.StartedAtUtc).TotalHours;
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
            .Where(static analysis => analysis.Occurrence.TargetHours.HasValue && analysis.Occurrence.TargetHours.Value < 24 && analysis.LatestCheckIn is not null)
            .ToList();
        var longer = history
            .Where(static analysis => analysis.Occurrence.TargetHours.HasValue && analysis.Occurrence.TargetHours.Value >= 24 && analysis.LatestCheckIn is not null)
            .ToList();

        if (shorter.Count < 2 || longer.Count < 2) {
            return null;
        }

        var shorterScore = shorter.Average(static analysis => GetToleranceScore(analysis.LatestCheckIn!));
        var longerScore = longer.Average(static analysis => GetToleranceScore(analysis.LatestCheckIn!));
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

        foreach (var symptom in history
                     .SelectMany(static analysis => analysis.Timeline)
                     .SelectMany(static checkIn => checkIn.Symptoms)) {
            symptomCounts[symptom] = symptomCounts.TryGetValue(symptom, out var count) ? count + 1 : 1;
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

    private static FastingMessageModel? BuildPositiveToleranceInsight(IReadOnlyList<FastingOccurrenceAnalysis> history) {
        var strongCheckIns = history
            .SelectMany(static analysis => analysis.Timeline)
            .Where(checkIn =>
                checkIn.EnergyLevel >= 4 &&
                checkIn.MoodLevel >= 4 &&
                !checkIn.Symptoms.Any(RiskySymptoms.Contains))
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

        var lowEnergy = latestCheckIn.EnergyLevel <= 2;
        var lowMood = latestCheckIn.MoodLevel <= 2;
        var hasRiskySymptom = latestCheckIn.Symptoms.Any(RiskySymptoms.Contains);

        return occurrence.EndedAtUtc is null && hasRiskySymptom && (lowEnergy || lowMood);
    }

    private static int CalculateCurrentStreak(IReadOnlyList<FastingOccurrence> completedOccurrences, DateTime todayUtcDate) {
        if (completedOccurrences.Count == 0) {
            return 0;
        }

        var streak = 0;
        var expectedDate = todayUtcDate;

        foreach (var occurrence in completedOccurrences.OrderByDescending(static occurrence => occurrence.StartedAtUtc)) {
            var occurrenceDate = occurrence.StartedAtUtc.Date;
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

        foreach (var symptom in analyses
                     .SelectMany(static analysis => analysis.Timeline)
                     .SelectMany(static checkIn => checkIn.Symptoms)) {
            symptomCounts[symptom] = symptomCounts.TryGetValue(symptom, out var count) ? count + 1 : 1;
        }

        return PrioritizedSymptoms.FirstOrDefault(symptom => symptomCounts.GetValueOrDefault(symptom) >= 2);
    }

    private static double GetToleranceScore(FastingCheckInSnapshot checkIn) => checkIn.EnergyLevel + checkIn.MoodLevel;

    private static IReadOnlyList<string> ParseSymptoms(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

    private sealed record FastingOccurrenceAnalysis(
        FastingOccurrence Occurrence,
        IReadOnlyList<FastingCheckInSnapshot> Timeline,
        FastingCheckInSnapshot? LatestCheckIn);

    private sealed record FastingCheckInSnapshot(
        DateTime CheckedInAtUtc,
        int HungerLevel,
        int EnergyLevel,
        int MoodLevel,
        IReadOnlyList<string> Symptoms,
        string? Notes);
}
