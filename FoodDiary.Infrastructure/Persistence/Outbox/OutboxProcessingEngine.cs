using System.Diagnostics;
using FoodDiary.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Infrastructure.Persistence.Outbox;

internal static class OutboxProcessingEngine {
    public static async Task<int> ProcessDueAsync<TMessage>(
        FoodDiaryDbContext context,
        DbSet<TMessage> messages,
        string tableName,
        string outboxName,
        int batchSize,
        TimeProvider timeProvider,
        Func<TMessage, CancellationToken, Task> dispatchAsync,
        Func<TMessage, object?> messageIdentity,
        ILogger logger,
        IQueryable<TMessage>? claimedQuery = null,
        CancellationToken cancellationToken = default)
        where TMessage : class, IOutboxMessage {
        if (batchSize <= 0) {
            return 0;
        }

        var stopwatch = Stopwatch.StartNew();
        try {
            DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;
            List<TMessage> claimed = await OutboxMessageClaimer
                .ClaimDueAsync(context, messages, tableName, batchSize, nowUtc, claimedQuery, cancellationToken)
                .ConfigureAwait(false);
            InfrastructureTelemetry.RecordOutboxMessages(outboxName, "claimed", claimed.Count);

            int processed = 0;
            int retried = 0;
            int deadLettered = 0;
            foreach (TMessage message in claimed) {
                try {
                    await dispatchAsync(message, cancellationToken).ConfigureAwait(false);
                    message.MarkProcessed(timeProvider.GetUtcNow().UtcDateTime);
                    processed++;
                } catch (Exception ex) {
                    if (HandleFailure(message, ex, outboxName, messageIdentity, timeProvider, logger)) {
                        deadLettered++;
                    } else {
                        retried++;
                    }
                }
            }

            if (claimed.Count > 0) {
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            InfrastructureTelemetry.RecordOutboxMessages(outboxName, "processed", processed);
            InfrastructureTelemetry.RecordOutboxMessages(outboxName, "retried", retried);
            InfrastructureTelemetry.RecordOutboxMessages(outboxName, "dead_lettered", deadLettered);

            DateTime? oldestCreatedOnUtc = await messages
                .AsNoTracking()
                .Where(message => message.ProcessedOnUtc == null && message.DeadLetteredOnUtc == null)
                .MinAsync(message => (DateTime?)message.CreatedOnUtc, cancellationToken)
                .ConfigureAwait(false);
            InfrastructureTelemetry.RecordOutboxOldestPendingAge(outboxName, nowUtc, oldestCreatedOnUtc);
            return processed;
        } finally {
            stopwatch.Stop();
            InfrastructureTelemetry.RecordOutboxProcessingDuration(outboxName, stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    private static bool HandleFailure<TMessage>(
        TMessage message,
        Exception exception,
        string outboxName,
        Func<TMessage, object?> messageIdentity,
        TimeProvider timeProvider,
        ILogger logger)
        where TMessage : IOutboxMessage {
        int attemptCount = message.AttemptCount + 1;
        string error = OutboxProcessingPolicy.TruncateError(exception.ToString());
        DateTime failedOnUtc = timeProvider.GetUtcNow().UtcDateTime;
        if (OutboxProcessingPolicy.ShouldDeadLetter(attemptCount)) {
            message.MarkDeadLettered(error, failedOnUtc);
            logger.LogError(
                exception,
                "{OutboxName} outbox dead-lettered {MessageIdentity} after {AttemptCount} attempts.",
                outboxName,
                messageIdentity(message),
                message.AttemptCount);
            return true;
        }

        message.MarkFailed(error, failedOnUtc.Add(OutboxProcessingPolicy.CalculateRetryDelay(attemptCount)));
        logger.LogWarning(
            exception,
            "{OutboxName} outbox failed for {MessageIdentity}. Attempt {AttemptCount} of {MaxAttemptCount}.",
            outboxName,
            messageIdentity(message),
            message.AttemptCount,
            OutboxProcessingPolicy.MaxAttemptCount);
        return false;
    }
}
