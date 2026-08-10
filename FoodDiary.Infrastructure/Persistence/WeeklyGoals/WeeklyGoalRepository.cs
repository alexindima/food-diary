using FoodDiary.Application.Abstractions.WeeklyGoals.Common;
using FoodDiary.Domain.Entities.WeeklyGoals;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.WeeklyGoals;

public sealed class WeeklyGoalRepository(FoodDiaryDbContext context) : IWeeklyGoalRepository {
    public Task<WeeklyGoal?> GetAsync(
        UserId userId,
        DateTime weekStartUtc,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        IQueryable<WeeklyGoal> query = context.WeeklyGoals;
        if (!asTracking) {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefaultAsync(
            goal => goal.UserId == userId && goal.WeekStartUtc == weekStartUtc,
            cancellationToken);
    }

    public async Task AddAsync(WeeklyGoal goal, CancellationToken cancellationToken = default) {
        await context.WeeklyGoals.AddAsync(goal, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WeeklyGoal>> GetReminderCandidatesAsync(
        DateTime earliestWeekStartUtc,
        DateTime latestWeekStartUtc,
        int limit,
        CancellationToken cancellationToken = default) {
        return await context.WeeklyGoals
            .Where(goal =>
                goal.ReminderEnabled &&
                goal.WeekStartUtc >= earliestWeekStartUtc &&
                goal.WeekStartUtc <= latestWeekStartUtc)
            .OrderBy(goal => goal.WeekStartUtc)
            .ThenBy(goal => goal.Id)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
