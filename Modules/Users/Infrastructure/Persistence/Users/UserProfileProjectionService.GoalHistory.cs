using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Users.Infrastructure.Persistence.Users;

public sealed partial class UserProfileProjectionService {
    private async Task<UserGoalTimeline> ReadGoalHistoryAsync(UserId userId, DateOnly from, DateOnly to, CancellationToken cancellationToken) {
        if (to < from || to.DayNumber - from.DayNumber > 366) {
            throw new ArgumentOutOfRangeException(nameof(to), "Goal history requires an ordered calendar range of at most 367 days.");
        }

        IQueryable<UserGoalRevision> revisions = AccessibleUsers.AsNoTracking().Where(user => user.Id == userId)
            .SelectMany(user => user.NutritionProfile.GoalHistory).AsNoTracking();
        // One baseline and at most one final revision per calendar day. Never load the aggregate or its complete history.
        UserGoalRevision? baseline = await revisions.Where(revision => revision.EffectiveFrom <= from)
            .OrderByDescending(revision => revision.Revision).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        List<UserGoalRevision> changes = await revisions.Where(revision => revision.EffectiveFrom > from && revision.EffectiveFrom <= to)
            .Where(revision => revision.Revision == revisions.Where(other => other.EffectiveFrom == revision.EffectiveFrom)
                .Max(other => other.Revision))
            .OrderBy(revision => revision.Revision).ToListAsync(cancellationToken).ConfigureAwait(false);
        var versions = new List<UserGoalVersion>(changes.Count + 1);
        if (baseline is not null) {
            versions.Add(new UserGoalVersion(baseline.Revision, baseline.EffectiveFrom, baseline.ToDailyGoals()));
        }
        versions.AddRange(changes.Select(revision => new UserGoalVersion(revision.Revision, revision.EffectiveFrom, revision.ToDailyGoals())));
        return new UserGoalTimeline(versions);
    }
}
