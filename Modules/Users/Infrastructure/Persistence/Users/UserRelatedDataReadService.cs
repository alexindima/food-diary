using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Users;

public sealed class UserRelatedDataReadService(FoodDiaryDbContext context) :
    IUserFastingReminderReadService, IUserCommentAuthorReadService {
    public async Task<IReadOnlyDictionary<UserId, UserFastingReminderModel>> GetReminderSettingsAsync(
        IReadOnlyCollection<UserId> userIds, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        if (userIds.Count == 0) {
            return new Dictionary<UserId, UserFastingReminderModel>();
        }

        UserId[] ids = [.. userIds.Distinct()];
        return await context.Users.AsNoTracking().Where(user => Enumerable.Contains(ids, user.Id))
            .Select(user => new { user.Id, user.FastingCheckInReminderHours, user.FastingCheckInFollowUpReminderHours })
            .ToDictionaryAsync(user => user.Id, user => new UserFastingReminderModel(
                user.FastingCheckInReminderHours, user.FastingCheckInFollowUpReminderHours), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<UserId, UserCommentAuthorModel>> GetAuthorsAsync(
        IReadOnlyCollection<UserId> userIds, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        if (userIds.Count == 0) {
            return new Dictionary<UserId, UserCommentAuthorModel>();
        }

        UserId[] ids = [.. userIds.Distinct()];
        return await context.Users.AsNoTracking().Where(user => Enumerable.Contains(ids, user.Id))
            .Select(user => new { user.Id, user.Username, user.FirstName })
            .ToDictionaryAsync(user => user.Id, user => new UserCommentAuthorModel(user.Username, user.FirstName), cancellationToken)
            .ConfigureAwait(false);
    }
}
