using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Queries.GetNotificationPreferences;

public sealed class GetNotificationPreferencesQueryHandler(INotificationPreferencesService notificationPreferencesService)
    : IQueryHandler<GetNotificationPreferencesQuery, Result<NotificationPreferencesModel>> {
    public async Task<Result<NotificationPreferencesModel>> Handle(
        GetNotificationPreferencesQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<NotificationPreferencesModel>(Errors.Authentication.InvalidToken);
        }

        return await notificationPreferencesService.GetAsync(new UserId(query.UserId.Value), cancellationToken).ConfigureAwait(false);
    }
}
