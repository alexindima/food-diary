using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Queries.GetNotificationPreferences;

public sealed class GetNotificationPreferencesQueryHandler(
    INotificationPreferencesService notificationPreferencesService,
    INotificationUserAccessService notificationUserAccessService)
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

        return await notificationPreferencesService.GetAsync(userIdResult.Value, cancellationToken).ConfigureAwait(false);
    }
}
