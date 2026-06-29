using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Common;

internal static class DietologistInvitationClientNotifier {
    public static Task NotifyAcceptedAsync(
        INotificationWriter notificationWriter,
        INotificationRepository notificationRepository,
        INotificationPusher notificationPusher,
        UserId clientUserId,
        string dietologistDisplayName,
        string invitationReferenceId,
        CancellationToken cancellationToken) =>
        NotifyAsync(
            notificationWriter,
            notificationRepository,
            notificationPusher,
            NotificationFactory.CreateDietologistInvitationAccepted(
                clientUserId,
                dietologistDisplayName,
                invitationReferenceId),
            cancellationToken);

    public static Task NotifyDeclinedAsync(
        INotificationWriter notificationWriter,
        INotificationRepository notificationRepository,
        INotificationPusher notificationPusher,
        UserId clientUserId,
        string dietologistDisplayName,
        string invitationReferenceId,
        CancellationToken cancellationToken) =>
        NotifyAsync(
            notificationWriter,
            notificationRepository,
            notificationPusher,
            NotificationFactory.CreateDietologistInvitationDeclined(
                clientUserId,
                dietologistDisplayName,
                invitationReferenceId),
            cancellationToken);

    private static async Task NotifyAsync(
        INotificationWriter notificationWriter,
        INotificationRepository notificationRepository,
        INotificationPusher notificationPusher,
        Notification notification,
        CancellationToken cancellationToken) {
        await notificationWriter.AddAsync(notification, sendWebPush: true, cancellationToken).ConfigureAwait(false);

        int unreadCount = await notificationRepository.GetUnreadCountAsync(notification.UserId, cancellationToken).ConfigureAwait(false);
        await notificationPusher.PushUnreadCountAsync(notification.UserId.Value, unreadCount, cancellationToken).ConfigureAwait(false);
        await notificationPusher.PushNotificationsChangedAsync(notification.UserId.Value, cancellationToken).ConfigureAwait(false);
    }
}
