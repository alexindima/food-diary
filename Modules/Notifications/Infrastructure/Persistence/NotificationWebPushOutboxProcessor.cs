using FoodDiary.Modules.Notifications.PersistenceModel;
using FoodDiary.Outbox.Infrastructure.Options;
using FoodDiary.Outbox.Infrastructure.Persistence;
using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Notifications.Infrastructure.Persistence;

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
