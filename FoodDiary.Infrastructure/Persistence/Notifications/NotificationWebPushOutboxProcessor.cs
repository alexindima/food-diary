using System.Diagnostics;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Infrastructure.Persistence.Outbox;
using FoodDiary.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Infrastructure.Persistence.Notifications;

internal sealed class NotificationWebPushOutboxProcessor(
    FoodDiaryDbContext context,
    IWebPushNotificationSender webPushNotificationSender,
    TimeProvider timeProvider,
    ILogger<NotificationWebPushOutboxProcessor> logger) : INotificationWebPushOutboxProcessor {
    private const string OutboxName = "notification_web_push";

    public async Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default) {
        if (batchSize <= 0) {
            return 0;
        }

        var stopwatch = Stopwatch.StartNew();
        try {
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
            InfrastructureTelemetry.RecordOutboxMessages(OutboxName, "claimed", messages.Count);

            int processed = 0;
            int retried = 0;
            int deadLettered = 0;
            foreach (NotificationWebPushOutboxMessage message in messages) {
                try {
                    await webPushNotificationSender.SendAsync(message.Notification, cancellationToken).ConfigureAwait(false);
                    message.MarkProcessed(timeProvider.GetUtcNow().UtcDateTime);
                    processed++;
                } catch (Exception ex) {
                    if (HandleFailure(message, ex)) {
                        deadLettered++;
                    } else {
                        retried++;
                    }
                }
            }

            await SaveAndRecordAsync(messages.Count, processed, retried, deadLettered, cancellationToken).ConfigureAwait(false);
            return processed;
        } finally {
            stopwatch.Stop();
            InfrastructureTelemetry.RecordOutboxProcessingDuration(OutboxName, stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    private bool HandleFailure(NotificationWebPushOutboxMessage message, Exception ex) {
        int attemptCount = message.AttemptCount + 1;
        string error = OutboxProcessingPolicy.TruncateError(ex.ToString());
        DateTime failedOnUtc = timeProvider.GetUtcNow().UtcDateTime;
        if (OutboxProcessingPolicy.ShouldDeadLetter(attemptCount)) {
            message.MarkDeadLettered(error, failedOnUtc);
            logger.LogError(ex, "Notification web-push outbox dead-lettered {NotificationId} after {AttemptCount} attempts.", message.NotificationId.Value, message.AttemptCount);
            return true;
        }

        TimeSpan retryDelay = OutboxProcessingPolicy.CalculateRetryDelay(attemptCount);
        message.MarkFailed(error, failedOnUtc.Add(retryDelay));
        logger.LogWarning(
            ex,
            "Notification web-push outbox failed for {NotificationId}. Attempt {AttemptCount} of {MaxAttemptCount}.",
            message.NotificationId.Value,
            message.AttemptCount,
            OutboxProcessingPolicy.MaxAttemptCount);
        return false;
    }

    private async Task SaveAndRecordAsync(int claimed, int processed, int retried, int deadLettered, CancellationToken cancellationToken) {
        if (claimed > 0) {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        InfrastructureTelemetry.RecordOutboxMessages(OutboxName, "processed", processed);
        InfrastructureTelemetry.RecordOutboxMessages(OutboxName, "retried", retried);
        InfrastructureTelemetry.RecordOutboxMessages(OutboxName, "dead_lettered", deadLettered);
    }
}
