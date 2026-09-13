using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure.Persistence.Notifications;

internal sealed class NotificationWebPushOutboxProcessor(
    DbContext context,
    DbSet<NotificationWebPushOutboxMessage> messages,
    IWebPushNotificationSender webPushNotificationSender,
    IOptions<OutboxProcessingOptions> options,
    TimeProvider timeProvider,
    ILogger<NotificationWebPushOutboxProcessor> logger, Action? ensureCleanEntry = null) : INotificationWebPushOutboxProcessor {
    public Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default) =>
        OutboxProcessingEngine.ProcessDueAsync(
            context,
            messages,
            "\"NotificationWebPushOutbox\"",
            "notification_web_push",
            batchSize,
            options.Value,
            timeProvider,
            (message, token) => webPushNotificationSender.SendAsync(message.Notification, token),
            static message => message.NotificationId.Value,
            logger,
            messages.Include(message => message.Notification),
            cancellationToken, ensureCleanEntry: ensureCleanEntry);
}
