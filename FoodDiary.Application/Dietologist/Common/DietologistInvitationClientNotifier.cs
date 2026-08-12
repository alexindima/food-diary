using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Common;

internal static class DietologistInvitationClientNotifier {
    public static Task NotifyAcceptedAsync(
        INotificationWriter notificationWriter,
        INotificationClientRefreshService notificationClientRefreshService,
            IPostCommitActionQueue postCommitActionQueue,
        UserId clientUserId,
        string dietologistDisplayName,
        string invitationReferenceId,
        CancellationToken cancellationToken) =>
        NotifyAsync(
            notificationWriter,
            notificationClientRefreshService,
            postCommitActionQueue,
            DietologistNotificationFactory.CreateInvitationAccepted(
                clientUserId,
                dietologistDisplayName,
                invitationReferenceId),
            cancellationToken);

    public static Task NotifyDeclinedAsync(
        INotificationWriter notificationWriter,
        INotificationClientRefreshService notificationClientRefreshService,
            IPostCommitActionQueue postCommitActionQueue,
        UserId clientUserId,
        string dietologistDisplayName,
        string invitationReferenceId,
        CancellationToken cancellationToken) =>
        NotifyAsync(
            notificationWriter,
            notificationClientRefreshService,
            postCommitActionQueue,
            DietologistNotificationFactory.CreateInvitationDeclined(
                clientUserId,
                dietologistDisplayName,
                invitationReferenceId),
            cancellationToken);

    private static async Task NotifyAsync(
        INotificationWriter notificationWriter,
        INotificationClientRefreshService notificationClientRefreshService,
            IPostCommitActionQueue postCommitActionQueue,
        Notification notification,
        CancellationToken cancellationToken) {
        await notificationWriter.AddAsync(notification, sendWebPush: true, cancellationToken).ConfigureAwait(false);
        DietologistNotificationPostCommitActions.EnqueueUnreadCountPush(
            postCommitActionQueue,
            notificationClientRefreshService,
            notification.UserId);
    }
}
