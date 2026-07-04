using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Queries.GetUnreadCount;

public class GetUnreadCountQueryHandler(
    INotificationReadRepository notificationRepository,
    INotificationUserContextService notificationUserContextService)
    : IQueryHandler<GetUnreadCountQuery, Result<int>> {
    public async Task<Result<int>> Handle(GetUnreadCountQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<int>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        Result<NotificationUserContext> contextResult = await notificationUserContextService.GetAsync(userId, cancellationToken).ConfigureAwait(false);
        if (contextResult.IsFailure) {
            return Result.Failure<int>(contextResult.Error);
        }

        int count = await notificationRepository.GetUnreadCountAsync(userId, cancellationToken).ConfigureAwait(false);
        if (contextResult.Value.HasPassword) {
            count -= await notificationRepository.GetUnreadCountAsync(userId, NotificationTypes.PasswordSetupSuggested, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success(count);
    }
}
