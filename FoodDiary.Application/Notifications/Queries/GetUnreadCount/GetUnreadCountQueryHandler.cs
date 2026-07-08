using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Queries.GetUnreadCount;

public sealed class GetUnreadCountQueryHandler(
    INotificationFeedReadService notificationFeedReadService,
    INotificationUserContextService notificationUserContextService,
    INotificationUserAccessService notificationUserAccessService)
    : IQueryHandler<GetUnreadCountQuery, Result<int>> {
    public async Task<Result<int>> Handle(GetUnreadCountQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            notificationUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<int>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Result<NotificationUserContext> contextResult = await notificationUserContextService.GetAsync(userId, cancellationToken).ConfigureAwait(false);
        if (contextResult.IsFailure) {
            return Result.Failure<int>(contextResult.Error);
        }

        int count = await notificationFeedReadService
            .GetVisibleUnreadCountAsync(userId, contextResult.Value, cancellationToken)
            .ConfigureAwait(false);
        return Result.Success(count);
    }
}
