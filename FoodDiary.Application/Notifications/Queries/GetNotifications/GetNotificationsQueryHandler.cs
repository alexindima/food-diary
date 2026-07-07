using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Queries.GetNotifications;

public sealed class GetNotificationsQueryHandler(
    INotificationUserContextService notificationUserContextService,
    INotificationFeedReadService notificationFeedReadService)
    : IQueryHandler<GetNotificationsQuery, Result<IReadOnlyList<NotificationModel>>> {
    public async Task<Result<IReadOnlyList<NotificationModel>>> Handle(
        GetNotificationsQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<IReadOnlyList<NotificationModel>>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Result<NotificationUserContext> contextResult = await notificationUserContextService.GetAsync(userId, cancellationToken).ConfigureAwait(false);
        if (contextResult.IsFailure) {
            return Result.Failure<IReadOnlyList<NotificationModel>>(contextResult.Error);
        }

        NotificationUserContext context = contextResult.Value;
        IReadOnlyList<NotificationModel> models = await notificationFeedReadService
            .GetVisibleNotificationsAsync(userId, context, cancellationToken)
            .ConfigureAwait(false);
        return Result.Success<IReadOnlyList<NotificationModel>>(models);
    }
}
