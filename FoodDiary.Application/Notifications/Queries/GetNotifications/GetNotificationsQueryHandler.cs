using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Notifications.Mappings;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Notifications;

namespace FoodDiary.Application.Notifications.Queries.GetNotifications;

public class GetNotificationsQueryHandler(
    INotificationReadRepository notificationRepository,
    INotificationUserContextService notificationUserContextService,
    INotificationTextRenderer notificationTextRenderer)
    : IQueryHandler<GetNotificationsQuery, Result<IReadOnlyList<NotificationModel>>> {
    public async Task<Result<IReadOnlyList<NotificationModel>>> Handle(
        GetNotificationsQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<IReadOnlyList<NotificationModel>>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        Result<NotificationUserContext> contextResult = await notificationUserContextService.GetAsync(userId, cancellationToken).ConfigureAwait(false);
        if (contextResult.IsFailure) {
            return Result.Failure<IReadOnlyList<NotificationModel>>(contextResult.Error);
        }

        NotificationUserContext context = contextResult.Value;
        IReadOnlyList<Notification> notifications = await notificationRepository.GetByUserAsync(userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        IEnumerable<Notification> visibleNotifications = context.HasPassword
            ? notifications.Where(notification => !string.Equals(notification.Type, NotificationTypes.PasswordSetupSuggested, StringComparison.Ordinal))
            : notifications;
        var models = visibleNotifications
            .Select(notification => notification.ToModel(
                notificationTextRenderer.RenderFromPayload(notification.Type, notification.PayloadJson, context.Language)))
            .ToList();
        return Result.Success<IReadOnlyList<NotificationModel>>(models);
    }
}
