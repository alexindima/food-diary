using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Notifications;

namespace FoodDiary.Application.Notifications.Services;

public sealed class NotificationWriter(
    INotificationWriteRepository notificationRepository,
    INotificationWebPushOutbox webPushOutbox) : INotificationWriter {
    public async Task AddAsync(
        Notification notification,
        bool sendWebPush = false,
        CancellationToken cancellationToken = default) {
        await notificationRepository.AddAsync(notification, cancellationToken).ConfigureAwait(false);

        if (sendWebPush) {
            await webPushOutbox.EnqueueAsync(notification.Id, cancellationToken).ConfigureAwait(false);
        }
    }
}
