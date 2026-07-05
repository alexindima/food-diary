using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Notifications.Mappings;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Services;

internal sealed class NotificationFeedReadService(
    INotificationReadRepository notificationRepository,
    INotificationTextRenderer notificationTextRenderer) : INotificationFeedReadService {
    public async Task<IReadOnlyList<NotificationModel>> GetVisibleNotificationsAsync(
        UserId userId,
        NotificationUserContext context,
        CancellationToken cancellationToken) {
        IReadOnlyList<Notification> notifications = await notificationRepository
            .GetByUserAsync(userId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        IEnumerable<Notification> visibleNotifications = context.HasPassword
            ? notifications.Where(notification => !string.Equals(notification.Type, NotificationTypes.PasswordSetupSuggested, StringComparison.Ordinal))
            : notifications;

        return [.. visibleNotifications.Select(notification => notification.ToModel(
            notificationTextRenderer.RenderFromPayload(notification.Type, notification.PayloadJson, context.Language)))];
    }

    public async Task<int> GetVisibleUnreadCountAsync(
        UserId userId,
        NotificationUserContext context,
        CancellationToken cancellationToken) {
        int count = await notificationRepository.GetUnreadCountAsync(userId, cancellationToken).ConfigureAwait(false);
        if (context.HasPassword) {
            count -= await notificationRepository
                .GetUnreadCountAsync(userId, NotificationTypes.PasswordSetupSuggested, cancellationToken)
                .ConfigureAwait(false);
        }

        return count;
    }
}
