using FoodDiary.Modules.Notifications.Domain.Entities;
using FoodDiary.Modules.Notifications.Application.Abstractions.Models;

namespace FoodDiary.Modules.Notifications.Application.Abstractions.Common;

public interface IWebPushNotificationSender {
    Task SendAsync(Notification notification, CancellationToken cancellationToken = default);

    async Task<WebPushDeliveryOutcome> SendBatchAsync(Notification notification, IReadOnlySet<Guid> completedSubscriptionIds,
        CancellationToken cancellationToken = default) {
        await SendAsync(notification, cancellationToken).ConfigureAwait(false);
        return WebPushDeliveryOutcome.Skipped;
    }
}
