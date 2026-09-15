using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Modules.Notifications.Application.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;

namespace FoodDiary.Modules.Notifications.Application.Queries.GetUnreadCount;

public sealed class GetUnreadCountQueryHandler(
    INotificationReadModelRepository notificationReadModelRepository,
    INotificationUserContextService notificationUserContextService,
    ICurrentUserAccessService notificationUserAccessService)
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

        int count = await GetVisibleUnreadCountAsync(userId, contextResult.Value, cancellationToken)
            .ConfigureAwait(false);
        return Result.Success(count);
    }
    private async Task<int> GetVisibleUnreadCountAsync(
        UserId userId,
        NotificationUserContext context,
        CancellationToken cancellationToken) {
        int count = await notificationReadModelRepository.GetUnreadCountAsync(userId, cancellationToken).ConfigureAwait(false);
        if (context.HasPassword) {
            count -= await notificationReadModelRepository
                .GetUnreadCountAsync(userId, NotificationTypes.PasswordSetupSuggested, cancellationToken)
                .ConfigureAwait(false);
        }

        return count;
    }
}
