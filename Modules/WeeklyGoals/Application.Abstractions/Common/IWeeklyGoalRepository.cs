using FoodDiary.Modules.WeeklyGoals.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.WeeklyGoals.Application.Abstractions.Common;

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
        int offset,
        int limit,
        CancellationToken cancellationToken = default);
}
