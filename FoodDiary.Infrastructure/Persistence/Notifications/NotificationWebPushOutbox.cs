using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Persistence.Notifications;

internal sealed class NotificationWebPushOutbox(
    FoodDiaryDbContext context,
    TimeProvider timeProvider) : INotificationWebPushOutbox {
    public async Task EnqueueAsync(NotificationId notificationId, CancellationToken cancellationToken = default) {
        var message = NotificationWebPushOutboxMessage.Create(
            notificationId,
            timeProvider.GetUtcNow().UtcDateTime);

        await context.NotificationWebPushOutbox.AddAsync(message, cancellationToken).ConfigureAwait(false);
    }
}
