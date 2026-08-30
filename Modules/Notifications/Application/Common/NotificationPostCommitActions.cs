using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Common;

internal static class NotificationPostCommitActions {
    public static void EnqueueUnreadCountPush(
        IPostCommitActionQueue postCommitActionQueue,
        INotificationClientRefreshService notificationClientRefreshService,
        UserId userId,
        bool pushChanged = true) {
        postCommitActionQueue.Enqueue("notifications.unread-count-push",
            cancellationToken => notificationClientRefreshService.RefreshAsync(
                userId,
                pushChanged,
                cancellationToken));
    }
}
