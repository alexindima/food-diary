using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Queries.GetFastingStats;

public class GetFastingStatsQueryHandler(
    IFastingSessionRepository fastingRepository,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetFastingStatsQuery, Result<FastingStatsModel>> {
    public async Task<Result<FastingStatsModel>> Handle(
        GetFastingStatsQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<FastingStatsModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        var totalCompleted = await fastingRepository.GetCompletedCountAsync(userId, cancellationToken);
        var currentStreak = await fastingRepository.GetCurrentStreakAsync(userId, cancellationToken);

        var now = dateTimeProvider.UtcNow;
        var last30Days = await fastingRepository.GetHistoryAsync(
            userId,
            now.AddDays(-30),
            now,
            cancellationToken);

        var completedSessions = last30Days.Where(s => s.IsCompleted && s.EndedAtUtc.HasValue).ToList();
        var avgDuration = completedSessions.Count > 0
            ? completedSessions.Average(s => (s.EndedAtUtc!.Value - s.StartedAtUtc).TotalHours)
            : 0;

        return Result.Success(new FastingStatsModel(totalCompleted, currentStreak, Math.Round(avgDuration, 1)));
    }
}
