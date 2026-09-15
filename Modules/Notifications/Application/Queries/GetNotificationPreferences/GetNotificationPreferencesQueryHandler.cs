using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Modules.Notifications.Application.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Notifications.Application.Mappings;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

namespace FoodDiary.Modules.Notifications.Application.Queries.GetNotificationPreferences;

public sealed class GetNotificationPreferencesQueryHandler(
    IUserNotificationProfileService userProfileService,
    ICurrentUserAccessService notificationUserAccessService)
    : IQueryHandler<GetNotificationPreferencesQuery, Result<NotificationPreferencesModel>> {
    public async Task<Result<NotificationPreferencesModel>> Handle(
        GetNotificationPreferencesQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            notificationUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<NotificationPreferencesModel>(userIdResult);
        }

        return await GetAsync(userIdResult.Value, cancellationToken).ConfigureAwait(false);
    }
    private async Task<Result<NotificationPreferencesModel>> GetAsync(UserId userId, CancellationToken cancellationToken = default) {
        Result<UserNotificationProfileModel> result = await userProfileService.GetAsync(userId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure) {
            return Result.Failure<NotificationPreferencesModel>(result.Error);
        }

        return Result.Success(NotificationPreferenceMappings.ToModel(result.Value));
    }
}
