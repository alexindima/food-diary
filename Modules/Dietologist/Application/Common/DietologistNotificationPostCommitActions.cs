using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Common;

internal static class DietologistNotificationPostCommitActions {
    public static void EnqueueUnreadCountPush(
        IPostCommitActionQueue postCommitActionQueue,
        INotificationClientRefreshService notificationClientRefreshService,
        UserId userId,
        bool pushChanged = true) {
        postCommitActionQueue.Enqueue("notifications.unread-count-push",
            cancellationToken => notificationClientRefreshService.RefreshAsync(userId, pushChanged, cancellationToken));
    }
}
