using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Modules.Notifications.Domain.Entities;
using FoodDiary.Modules.Notifications.Application.Abstractions.Models;

namespace FoodDiary.Modules.Notifications.Application.Services;

public sealed class NotificationWriter(
    INotificationWriteRepository notificationRepository,
    INotificationWebPushOutbox webPushOutbox,
    IWebPushDeliveryAudienceService deliveryAudienceService,
    TimeProvider timeProvider) : INotificationWriter {
    public async Task AddAsync(
        NotificationRequest request,
        bool sendWebPush = false,
        CancellationToken cancellationToken = default) {
        NotificationIntent intent = request.Intent;
        var notification = Notification.Create(request.UserId, intent.Type, intent.PayloadJson, intent.ReferenceId);
        await notificationRepository.AddAsync(notification, cancellationToken).ConfigureAwait(false);

        if (!sendWebPush) {
            return;
        }

        IReadOnlyList<WebPushDeliverySubscription> audience = await deliveryAudienceService.GetActiveAudienceAsync(
            notification.UserId, notification.Type, timeProvider.GetUtcNow().UtcDateTime, cancellationToken).ConfigureAwait(false);
        if (audience.Count > 0) {
            await webPushOutbox.EnqueueAsync(notification.Id, cancellationToken).ConfigureAwait(false);
        }
    }
}
