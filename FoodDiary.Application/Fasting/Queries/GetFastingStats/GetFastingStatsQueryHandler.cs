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
    public async Task<Result<FastingStatsModel>> Handle(
        GetFastingStatsQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<FastingStatsModel>(Errors.Authentication.InvalidToken);
        }

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

        return Result.Success(new FastingStatsModel(totalCompleted, currentStreak, Math.Round(avgDuration, 1)));
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
}
