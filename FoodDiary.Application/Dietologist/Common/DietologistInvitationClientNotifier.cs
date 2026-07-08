using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Common;

internal static class DietologistInvitationClientNotifier {
    public static Task NotifyAcceptedAsync(
        INotificationWriter notificationWriter,
        INotificationReadModelRepository notificationReadModelRepository,
        INotificationPusher notificationPusher,
        IPostCommitActionQueue postCommitActionQueue,
        UserId clientUserId,
        string dietologistDisplayName,
        string invitationReferenceId,
        CancellationToken cancellationToken) =>
        NotifyAsync(
            notificationWriter,
            notificationReadModelRepository,
            notificationPusher,
            postCommitActionQueue,
            NotificationFactory.CreateDietologistInvitationAccepted(
                clientUserId,
                dietologistDisplayName,
                invitationReferenceId),
            cancellationToken);

    public static Task NotifyDeclinedAsync(
        INotificationWriter notificationWriter,
        INotificationReadModelRepository notificationReadModelRepository,
        INotificationPusher notificationPusher,
        IPostCommitActionQueue postCommitActionQueue,
        UserId clientUserId,
        string dietologistDisplayName,
        string invitationReferenceId,
        CancellationToken cancellationToken) =>
        NotifyAsync(
            notificationWriter,
            notificationReadModelRepository,
            notificationPusher,
            postCommitActionQueue,
            NotificationFactory.CreateDietologistInvitationDeclined(
                clientUserId,
                dietologistDisplayName,
                invitationReferenceId),
            cancellationToken);

    private static async Task NotifyAsync(
        INotificationWriter notificationWriter,
        INotificationReadModelRepository notificationReadModelRepository,
        INotificationPusher notificationPusher,
        IPostCommitActionQueue postCommitActionQueue,
        Notification notification,
        CancellationToken cancellationToken) {
        await notificationWriter.AddAsync(notification, sendWebPush: true, cancellationToken).ConfigureAwait(false);
        NotificationPostCommitActions.EnqueueUnreadCountPush(
            postCommitActionQueue,
            notificationReadModelRepository,
            notificationPusher,
            notification.UserId);
    }
}
