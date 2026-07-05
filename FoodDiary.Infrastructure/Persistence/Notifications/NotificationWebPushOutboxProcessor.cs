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
    private const int MaxErrorLength = 2048;

    public async Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default) {
        if (batchSize <= 0) {
            return 0;
        }

        DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        List<NotificationWebPushOutboxMessage> messages = await OutboxMessageClaimer
            .ClaimDueAsync(
                context,
                context.NotificationWebPushOutbox,
                "\"NotificationWebPushOutbox\"",
                batchSize,
                nowUtc,
                context.NotificationWebPushOutbox.Include(message => message.Notification),
                cancellationToken)
            .ConfigureAwait(false);

        int processed = 0;
        foreach (NotificationWebPushOutboxMessage message in messages) {
            try {
                await webPushNotificationSender.SendAsync(message.Notification, cancellationToken).ConfigureAwait(false);
                message.MarkProcessed(timeProvider.GetUtcNow().UtcDateTime);
                processed++;
            } catch (Exception ex) {
                TimeSpan retryDelay = CalculateRetryDelay(message.AttemptCount + 1);
                message.MarkFailed(TruncateError(ex.ToString()), timeProvider.GetUtcNow().UtcDateTime.Add(retryDelay));
                logger.LogWarning(
                    ex,
                    "Notification web-push outbox failed for {NotificationId}. Attempt {AttemptCount}.",
                    message.NotificationId.Value,
                    message.AttemptCount);
            }
        }

        if (messages.Count > 0) {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return processed;
    }

    private static TimeSpan CalculateRetryDelay(int attemptCount) {
        int minutes = Math.Min(60, (int)Math.Pow(2, Math.Min(attemptCount, 6)));
        return TimeSpan.FromMinutes(minutes);
    }

    private static string TruncateError(string error) =>
        error.Length <= MaxErrorLength ? error : error[..MaxErrorLength];
}
