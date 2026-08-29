using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Fasting.Application.Services;

internal static class FastingNotificationPushDispatcher {
    public static async Task PushAsync(
        IReadOnlyCollection<UserId> usersToPush,
        INotificationClientRefreshService notificationClientRefreshService,
        CancellationToken cancellationToken) {
        foreach (UserId userId in usersToPush) {
            await notificationClientRefreshService
                .RefreshAsync(userId, pushChanged: true, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
