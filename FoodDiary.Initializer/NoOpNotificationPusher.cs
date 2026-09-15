using FoodDiary.Modules.Notifications.Application.Abstractions.Common;

namespace FoodDiary.Initializer;

internal sealed class NoOpNotificationPusher : INotificationPusher {
    public Task PushUnreadCountAsync(Guid userId, int count, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task PushNotificationsChangedAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
