using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Infrastructure.Persistence.Notifications;

internal sealed class NotificationWebPushOutboxProcessor(
    FoodDiaryDbContext context,
    IWebPushNotificationSender webPushNotificationSender,
    TimeProvider timeProvider,
    ILogger<NotificationWebPushOutboxProcessor> logger) : INotificationWebPushOutboxProcessor {
    public Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default) =>
        OutboxProcessingEngine.ProcessDueAsync(
            context,
            context.NotificationWebPushOutbox,
            "\"NotificationWebPushOutbox\"",
            "notification_web_push",
            batchSize,
            timeProvider,
            (message, token) => webPushNotificationSender.SendAsync(message.Notification, token),
            static message => message.NotificationId.Value,
            logger,
            context.NotificationWebPushOutbox.Include(message => message.Notification),
            cancellationToken);
}
