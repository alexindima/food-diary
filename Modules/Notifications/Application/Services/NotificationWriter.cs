using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Notifications;

namespace FoodDiary.Application.Notifications.Services;

public sealed class NotificationWriter(
    INotificationWriteRepository notificationRepository,
    INotificationWebPushOutbox webPushOutbox) : INotificationWriter {
    public async Task AddAsync(
        NotificationRequest request,
        bool sendWebPush = false,
        CancellationToken cancellationToken = default) {
        var notification = Notification.Create(request.UserId, request.Type, request.PayloadJson, request.ReferenceId);
        await notificationRepository.AddAsync(notification, cancellationToken).ConfigureAwait(false);

        if (sendWebPush) {
            await webPushOutbox.EnqueueAsync(notification.Id, cancellationToken).ConfigureAwait(false);
        }
    }
}
