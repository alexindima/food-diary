using FoodDiary.Modules.Notifications.Application.Mappings;
using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Modules.Notifications.Application.Abstractions.Models;
using FoodDiary.Modules.Notifications.Application.Common;
using FoodDiary.Modules.Notifications.Application.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;

namespace FoodDiary.Modules.Notifications.Application.Queries.GetNotifications;

public sealed class GetNotificationsQueryHandler(
    INotificationUserContextService notificationUserContextService,
    INotificationReadModelRepository notificationReadModelRepository, INotificationTextRenderer notificationTextRenderer,
    ICurrentUserAccessService notificationUserAccessService)
    : IQueryHandler<GetNotificationsQuery, Result<IReadOnlyList<NotificationModel>>> {
    public async Task<Result<IReadOnlyList<NotificationModel>>> Handle(
        GetNotificationsQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            notificationUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<NotificationModel>>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Result<NotificationUserContext> contextResult = await notificationUserContextService.GetAsync(userId, cancellationToken).ConfigureAwait(false);
        if (contextResult.IsFailure) {
            return Result.Failure<IReadOnlyList<NotificationModel>>(contextResult.Error);
        }

        NotificationUserContext context = contextResult.Value;
        IReadOnlyList<NotificationModel> models = await GetVisibleNotificationsAsync(userId, context, cancellationToken)
            .ConfigureAwait(false);
        return Result.Success<IReadOnlyList<NotificationModel>>(models);
    }
    private async Task<IReadOnlyList<NotificationModel>> GetVisibleNotificationsAsync(
        UserId userId,
        NotificationUserContext context,
        CancellationToken cancellationToken) {
        IReadOnlyList<NotificationReadModel> notifications = await notificationReadModelRepository
            .GetByUserReadModelsAsync(userId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        IEnumerable<NotificationReadModel> visibleNotifications = context.HasPassword
            ? notifications.Where(notification => !string.Equals(notification.Type, NotificationTypes.PasswordSetupSuggested, StringComparison.Ordinal))
            : notifications;

        return [.. visibleNotifications.Select(notification => notification.ToModel(
            notificationTextRenderer.RenderFromPayload(notification.Type, notification.PayloadJson, context.Language)))];
    }
}
