using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Application.Common;

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
