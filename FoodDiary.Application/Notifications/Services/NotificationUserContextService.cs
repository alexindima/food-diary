using FoodDiary.Results;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Services;

public sealed class NotificationUserContextService(INotificationUserAccessService notificationUserAccessService) : INotificationUserContextService {
    public async Task<Result<NotificationUserContext>> GetAsync(UserId userId, CancellationToken cancellationToken = default) {
        Result<User> userResult = await notificationUserAccessService.GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<NotificationUserContext>(userResult.Error);
        }

        User user = userResult.Value;
        return Result.Success(new NotificationUserContext(user.Id, user.HasPassword, user.Language));
    }
}
