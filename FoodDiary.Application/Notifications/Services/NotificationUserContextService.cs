using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Services;

public sealed class NotificationUserContextService(IUserNotificationProfileService userProfileService) : INotificationUserContextService {
    public async Task<Result<NotificationUserContext>> GetAsync(UserId userId, CancellationToken cancellationToken = default) {
        Result<UserNotificationProfileModel> result = await userProfileService.GetAsync(userId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure) {
            return Result.Failure<NotificationUserContext>(result.Error);
        }

        UserNotificationProfileModel user = result.Value;
        return Result.Success(new NotificationUserContext(user.UserId, user.HasPassword, user.Language));
    }
}
