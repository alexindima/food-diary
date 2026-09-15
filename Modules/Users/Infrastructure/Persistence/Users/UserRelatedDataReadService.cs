using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Users.Infrastructure.Persistence.Users;

public sealed class UserRelatedDataReadService(DbSet<User> users, Func<CancellationToken, Task>? synchronizeTransactionAsync = null) :
    IUserFastingReminderReadService, IUserCommentAuthorReadService {
    public async Task<IReadOnlyDictionary<UserId, UserFastingReminderModel>> GetReminderSettingsAsync(
        IReadOnlyCollection<UserId> userIds, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        cancellationToken.ThrowIfCancellationRequested();
        if (userIds.Count == 0) {
            return new Dictionary<UserId, UserFastingReminderModel>();
        }

        UserId[] ids = [.. userIds.Distinct()];
        return await users.AsNoTracking().Where(user => Enumerable.Contains(ids, user.Id))
            .Select(user => new { user.Id, user.FastingCheckInReminderHours, user.FastingCheckInFollowUpReminderHours })
            .ToDictionaryAsync(user => user.Id, user => new UserFastingReminderModel(
                user.FastingCheckInReminderHours, user.FastingCheckInFollowUpReminderHours), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<UserId, UserCommentAuthorModel>> GetAuthorsAsync(
        IReadOnlyCollection<UserId> userIds, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        cancellationToken.ThrowIfCancellationRequested();
        if (userIds.Count == 0) {
            return new Dictionary<UserId, UserCommentAuthorModel>();
        }

        UserId[] ids = [.. userIds.Distinct()];
        return await users.AsNoTracking().Where(user => Enumerable.Contains(ids, user.Id))
            .Select(user => new { user.Id, user.Username, user.FirstName })
            .ToDictionaryAsync(user => user.Id, user => new UserCommentAuthorModel(user.Username, user.FirstName), cancellationToken)
            .ConfigureAwait(false);
    }
}
