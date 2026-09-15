using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Notifications.Application.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Application.Services;

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
