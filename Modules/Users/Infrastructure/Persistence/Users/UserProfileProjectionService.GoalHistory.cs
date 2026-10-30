using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Users.Infrastructure.Persistence.Users;

public sealed partial class UserProfileProjectionService {
    private async Task<UserGoalTimeline> ReadGoalHistoryAsync(UserId userId, DateOnly from, DateOnly to, CancellationToken cancellationToken) {
        List<UserGoalRevision> revisions = await GetGoalHistoryQuery(userId, from, to)
            .AsNoTracking()
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return ToGoalTimeline(revisions);
    }

    private IQueryable<UserGoalRevision> GetGoalHistoryQuery(UserId userId, DateOnly from, DateOnly to) {
        if (to < from || to.DayNumber - from.DayNumber > 366) {
            throw new ArgumentOutOfRangeException(nameof(to), "Goal history requires an ordered calendar range of at most 367 days.");
        }

        IQueryable<UserGoalRevision> revisions = AccessibleUsers.AsNoTracking().Where(user => user.Id == userId)
            .SelectMany(user => user.NutritionProfile.GoalHistory).AsNoTracking();
        // Select the baseline and the final change for each in-range day in one bounded query.
        return revisions.Where(revision =>
                (revision.EffectiveFrom <= from && revision.Revision == revisions
                    .Where(other => other.EffectiveFrom <= from).Max(other => (long?)other.Revision)) ||
                (revision.EffectiveFrom > from && revision.EffectiveFrom <= to && revision.Revision == revisions
                    .Where(other => other.EffectiveFrom == revision.EffectiveFrom).Max(other => other.Revision)))
            .OrderBy(revision => revision.EffectiveFrom > from)
            .ThenBy(revision => revision.Revision);
    }

    private static UserGoalTimeline ToGoalTimeline(IEnumerable<UserGoalRevision> revisions) =>
        new([.. revisions.Select(revision => new UserGoalVersion(revision.Revision, revision.EffectiveFrom, revision.ToDailyGoals()))]);
}
