using FoodDiary.Modules.Notifications.PersistenceModel;
using FoodDiary.Modules.Notifications.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using FoodDiary.Modules.Notifications.Application.Abstractions.Common;

namespace FoodDiary.Modules.Notifications.Infrastructure.Persistence;

internal sealed class NotificationWebPushOutbox(
    DbSet<NotificationWebPushOutboxMessage> messages,
    TimeProvider timeProvider) : INotificationWebPushOutbox {
    public async Task EnqueueAsync(NotificationId notificationId, CancellationToken cancellationToken = default) {
        var message = NotificationWebPushOutboxMessage.Create(
            notificationId,
            timeProvider.GetUtcNow().UtcDateTime);

        await messages.AddAsync(message, cancellationToken).ConfigureAwait(false);
    }
}
