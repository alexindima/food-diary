using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Queries.GetFastingStats;

public class GetFastingStatsQueryHandler(
    IFastingOccurrenceRepository fastingOccurrenceRepository,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetFastingStatsQuery, Result<FastingStatsModel>> {
    private static readonly string[] PrioritizedSymptoms = ["dizziness", "weakness", "headache", "irritability", "cravings"];

    public async Task<Result<FastingStatsModel>> Handle(
        GetFastingStatsQuery query, CancellationToken cancellationToken) {
        var userId = new UserId(query.UserId!.Value);
        var allOccurrences = await fastingOccurrenceRepository.GetByUserAsync(
            userId,
            cancellationToken: cancellationToken);
        var completedOccurrences = allOccurrences
            .Where(static occurrence => occurrence.Status == FastingOccurrenceStatus.Completed && occurrence.EndedAtUtc.HasValue)
            .ToList();

        var totalCompleted = completedOccurrences.Count;
        var currentStreak = CalculateCurrentStreak(completedOccurrences, dateTimeProvider.UtcNow.Date);

        var now = dateTimeProvider.UtcNow;
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
        var topSymptom = GetTopSymptom(last30Days);

        return Result.Success(new FastingStatsModel(
            totalCompleted,
            currentStreak,
            Math.Round(avgDuration, 1),
            completionRate,
            checkInRate,
            lastCheckInAtUtc,
            topSymptom));
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

    private static string? GetTopSymptom(IReadOnlyList<FastingOccurrence> occurrences) {
        var symptomCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var occurrence in occurrences) {
            foreach (var symptom in GetSymptoms(occurrence)) {
                symptomCounts[symptom] = symptomCounts.TryGetValue(symptom, out var count) ? count + 1 : 1;
            }
        }

        return PrioritizedSymptoms.FirstOrDefault(symptom => symptomCounts.GetValueOrDefault(symptom) >= 2);
    }

    private static IReadOnlyList<string> GetSymptoms(FastingOccurrence occurrence) =>
        string.IsNullOrWhiteSpace(occurrence.Symptoms)
            ? []
            : occurrence.Symptoms.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
