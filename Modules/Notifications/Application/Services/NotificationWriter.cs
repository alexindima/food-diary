using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Modules.Notifications.Domain.Entities;

namespace FoodDiary.Modules.Notifications.Application.Services;

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
