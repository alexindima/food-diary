using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Notifications;

namespace FoodDiary.Application.Notifications.Services;

public sealed class NotificationWriter(
    INotificationRepository notificationRepository,
    IWebPushNotificationSender webPushNotificationSender,
    IUnitOfWork unitOfWork) : INotificationWriter {
    public async Task AddAsync(
        Notification notification,
        bool sendWebPush = false,
        CancellationToken cancellationToken = default) {
        await notificationRepository.AddAsync(notification, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (sendWebPush) {
            await webPushNotificationSender.SendAsync(notification, cancellationToken).ConfigureAwait(false);
        }
    }
}
