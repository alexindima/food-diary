using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Common;

internal static class DietologistInvitationClientNotifier {
    public static Task NotifyAcceptedAsync(
        INotificationRepository notificationRepository,
        INotificationPusher notificationPusher,
        IWebPushNotificationSender webPushNotificationSender,
        UserId clientUserId,
        string dietologistDisplayName,
        string invitationReferenceId,
        CancellationToken cancellationToken) =>
        NotifyAsync(
            notificationRepository,
            notificationPusher,
            webPushNotificationSender,
            NotificationFactory.CreateDietologistInvitationAccepted(
                clientUserId,
                dietologistDisplayName,
                invitationReferenceId),
            cancellationToken);

    public static Task NotifyDeclinedAsync(
        INotificationRepository notificationRepository,
        INotificationPusher notificationPusher,
        IWebPushNotificationSender webPushNotificationSender,
        UserId clientUserId,
        string dietologistDisplayName,
        string invitationReferenceId,
        CancellationToken cancellationToken) =>
        NotifyAsync(
            notificationRepository,
            notificationPusher,
            webPushNotificationSender,
            NotificationFactory.CreateDietologistInvitationDeclined(
                clientUserId,
                dietologistDisplayName,
                invitationReferenceId),
            cancellationToken);

    private static async Task NotifyAsync(
        INotificationRepository notificationRepository,
        INotificationPusher notificationPusher,
        IWebPushNotificationSender webPushNotificationSender,
        Notification notification,
        CancellationToken cancellationToken) {
        await notificationRepository.AddAsync(notification, cancellationToken);
        await webPushNotificationSender.SendAsync(notification, cancellationToken);

        var unreadCount = await notificationRepository.GetUnreadCountAsync(notification.UserId, cancellationToken);
        await notificationPusher.PushUnreadCountAsync(notification.UserId.Value, unreadCount, cancellationToken);
        await notificationPusher.PushNotificationsChangedAsync(notification.UserId.Value, cancellationToken);
    }
}
