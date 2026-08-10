using FoodDiary.Domain.Entities.WeeklyGoals;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.WeeklyGoals.Common;

public interface IWeeklyGoalRepository {
    Task<WeeklyGoal?> GetAsync(
        UserId userId,
        DateTime weekStartUtc,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task AddAsync(WeeklyGoal goal, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WeeklyGoal>> GetReminderCandidatesAsync(
        DateTime earliestWeekStartUtc,
        DateTime latestWeekStartUtc,
        int limit,
        CancellationToken cancellationToken = default);
}
