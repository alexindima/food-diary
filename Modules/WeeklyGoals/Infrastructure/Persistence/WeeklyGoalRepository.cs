using System.Data.Common;
using FoodDiary.Modules.WeeklyGoals.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.WeeklyGoals.Common;
using FoodDiary.Domain.Entities.WeeklyGoals;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.WeeklyGoals;

public sealed class WeeklyGoalRepository(WeeklyGoalsDbContext context, Func<DbTransaction?> currentTransaction) : IWeeklyGoalRepository {
    public async Task<WeeklyGoal?> GetAsync(
        UserId userId,
        DateTime weekStartUtc,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        await SynchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        IQueryable<WeeklyGoal> query = context.WeeklyGoals;
        if (!asTracking) {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(
            goal => goal.UserId == userId && goal.WeekStartUtc == weekStartUtc,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task AddAsync(WeeklyGoal goal, CancellationToken cancellationToken = default) {
        await context.WeeklyGoals.AddAsync(goal, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WeeklyGoal>> GetReminderCandidatesAsync(
        DateTime earliestWeekStartUtc,
        DateTime latestWeekStartUtc,
        int offset,
        int limit,
        CancellationToken cancellationToken = default) {
        await SynchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        return await context.WeeklyGoals
            .Where(goal =>
                goal.ReminderEnabled &&
                goal.WeekStartUtc >= earliestWeekStartUtc &&
                goal.WeekStartUtc <= latestWeekStartUtc)
            .OrderBy(goal => goal.WeekStartUtc)
            .ThenBy(goal => goal.Id)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task SynchronizeTransactionAsync(CancellationToken cancellationToken) {
        if (context.Database.IsRelational()) {
            await context.Database.UseTransactionAsync(currentTransaction(), cancellationToken).ConfigureAwait(false);
        }
    }
}
