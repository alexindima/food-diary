using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Queries.GetFastingOverview;

public sealed class GetFastingOverviewQueryHandler(
    IFastingOccurrenceRepository fastingOccurrenceRepository,
    IFastingCheckInRepository fastingCheckInRepository,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetFastingOverviewQuery, Result<FastingOverviewModel>> {
    private static readonly HashSet<string> RiskySymptoms = ["dizziness", "weakness"];
    private static readonly string[] PrioritizedSymptoms = ["dizziness", "weakness", "headache", "irritability", "cravings"];
    private const int AnalysisDays = 90;
    private const int HistoryPageSize = 10;

    public async Task<Result<FastingOverviewModel>> Handle(GetFastingOverviewQuery query, CancellationToken cancellationToken) {
        var userId = new UserId(query.UserId!.Value);
        var now = dateTimeProvider.UtcNow;
        var current = await fastingOccurrenceRepository.GetCurrentAsync(userId, cancellationToken: cancellationToken);
        var currentCheckIns = current is null
            ? []
            : await fastingCheckInRepository.GetByOccurrenceIdsAsync([current.Id], cancellationToken);
        var stats = await BuildStatsAsync(userId, now, cancellationToken);
        var insights = await BuildInsightsAsync(userId, now, current, cancellationToken);
        var history = await BuildHistoryAsync(userId, now, cancellationToken);

        return Result.Success(new FastingOverviewModel(
            current?.ToModel(current.Plan, currentCheckIns),
            stats,
            insights,
            history));
    }

    private async Task<FastingStatsModel> BuildStatsAsync(UserId userId, DateTime now, CancellationToken cancellationToken) {
        var allOccurrences = await fastingOccurrenceRepository.GetByUserAsync(userId, cancellationToken: cancellationToken);
        var completedOccurrences = allOccurrences
            .Where(static occurrence => occurrence.Status == FastingOccurrenceStatus.Completed && occurrence.EndedAtUtc.HasValue)
            .ToList();

        var totalCompleted = completedOccurrences.Count;
        var currentStreak = CalculateCurrentStreak(completedOccurrences, now.Date);

        var last30Days = await fastingOccurrenceRepository.GetByUserAsync(
            userId,
            from: now.AddDays(-30),
            to: now,
            cancellationToken: cancellationToken);

        var completedLast30Days = last30Days
            .Where(static occurrence => occurrence.Status == FastingOccurrenceStatus.Completed && occurrence.EndedAtUtc.HasValue)
            .ToList();

        var avgDuration = completedLast30Days.Count > 0
            ? completedLast30Days.Average(occurrence => (occurrence.EndedAtUtc!.Value - occurrence.StartedAtUtc).TotalHours)
            : 0;
        var completionRate = last30Days.Count > 0
            ? Math.Round(completedLast30Days.Count / (double)last30Days.Count * 100, 1)
            : 0;
        var checkInRate = last30Days.Count > 0
            ? Math.Round(last30Days.Count(static occurrence => occurrence.CheckInAtUtc.HasValue) / (double)last30Days.Count * 100, 1)
            : 0;
        var lastCheckInAtUtc = allOccurrences
            .Where(static occurrence => occurrence.CheckInAtUtc.HasValue)
            .MaxBy(static occurrence => occurrence.CheckInAtUtc)?
            .CheckInAtUtc;
        var topSymptom = PrioritizedSymptoms.FirstOrDefault(symptom =>
            last30Days.Sum(occurrence => GetSymptoms(occurrence).Count(current => string.Equals(current, symptom, StringComparison.OrdinalIgnoreCase))) >= 2);

        return new FastingStatsModel(
            totalCompleted,
            currentStreak,
            Math.Round(avgDuration, 1),
            completionRate,
            checkInRate,
            lastCheckInAtUtc,
            topSymptom);
    }

    private async Task<FastingInsightsModel> BuildInsightsAsync(
        UserId userId,
        DateTime now,
        FastingOccurrence? current,
        CancellationToken cancellationToken) {
        var history = await fastingOccurrenceRepository.GetByUserAsync(
            userId,
            from: now.AddDays(-AnalysisDays),
            to: now,
            cancellationToken: cancellationToken);
        var historyWithCheckIns = history.Where(static occurrence => occurrence.CheckInAtUtc.HasValue).ToList();

        var alerts = BuildAlerts(current, now);
        var insights = BuildInsights(historyWithCheckIns);
        return new FastingInsightsModel(alerts, insights);
    }

    private async Task<PagedResponse<FastingSessionModel>> BuildHistoryAsync(
        UserId userId,
        DateTime now,
        CancellationToken cancellationToken) {
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var from = currentMonthStart.AddMonths(-1);
        var to = currentMonthStart.AddMonths(2).AddDays(-1);
        var (occurrences, totalItems) = await fastingOccurrenceRepository.GetPagedByUserAsync(
            userId,
            from: from,
            to: to,
            page: 1,
            limit: HistoryPageSize,
            cancellationToken: cancellationToken);

        var occurrenceIds = occurrences.Select(static occurrence => occurrence.Id).ToArray();
        var checkIns = await fastingCheckInRepository.GetByOccurrenceIdsAsync(occurrenceIds, cancellationToken);
        var checkInsByOccurrence = checkIns
            .GroupBy(static checkIn => checkIn.OccurrenceId)
            .ToDictionary(static group => group.Key, static group => (IReadOnlyList<FastingCheckIn>)group.ToList());
        var models = occurrences
            .Select(occurrence => occurrence.ToModel(
                occurrence.Plan,
                checkInsByOccurrence.GetValueOrDefault(occurrence.Id)))
            .ToList();
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)HistoryPageSize);

        return new PagedResponse<FastingSessionModel>(models, 1, HistoryPageSize, totalPages, totalItems);
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

    private static bool HasRiskyCurrentCheckIn(FastingOccurrence occurrence) {
        var lowEnergy = (occurrence.EnergyLevel ?? 5) <= 2;
        var lowMood = (occurrence.MoodLevel ?? 5) <= 2;
        var hasRiskySymptom = GetSymptoms(occurrence).Any(RiskySymptoms.Contains);

        return occurrence.CheckInAtUtc.HasValue && hasRiskySymptom && (lowEnergy || lowMood);
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
