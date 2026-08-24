using System.Diagnostics;
using FoodDiary.Infrastructure.Options;
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
        OutboxProcessingOptions options,
        TimeProvider timeProvider,
        Func<TMessage, CancellationToken, Task> dispatchAsync,
        Func<TMessage, object?> messageIdentity,
        ILogger logger,
        IQueryable<TMessage>? claimedQuery = null,
        CancellationToken cancellationToken = default,
        Func<TMessage, DateTime, CancellationToken, Task<bool>>? tryMarkProcessedAsync = null)
        where TMessage : class, IOutboxMessage {
        if (batchSize <= 0) {
            return 0;
        }

        if (!OutboxProcessingOptions.HasValidConfiguration(options)) {
            throw new ArgumentException("Outbox processing durations are invalid.", nameof(options));
        }

        var stopwatch = Stopwatch.StartNew();
        try {
            int processed = 0;
            for (int i = 0; i < batchSize; i++) {
                cancellationToken.ThrowIfCancellationRequested();
                DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;
                OutboxClaimBatch<TMessage> claim = await OutboxMessageClaimer
                    .ClaimDueAsync(
                        context,
                        messages,
                        tableName,
                        batchSize: 1,
                        nowUtc,
                        options.LeaseDuration,
                        claimedQuery,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (claim.Messages.Count == 0) {
                    break;
                }

                InfrastructureTelemetry.RecordOutboxMessages(outboxName, "claimed", claim.Messages.Count);
                InfrastructureTelemetry.RecordOutboxMessages(outboxName, "reclaimed", claim.ReclaimedCount);
                if (await ProcessClaimedMessageAsync(
                        context,
                        claim.Messages[0],
                        outboxName,
                        options,
                        timeProvider,
                        dispatchAsync,
                        messageIdentity,
                        logger,
                        tryMarkProcessedAsync,
                        cancellationToken).ConfigureAwait(false)) {
                    processed++;
                }
            }

            DateTime observedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            DateTime? oldestCreatedOnUtc = await messages
                .AsNoTracking()
                .Where(message => message.ProcessedOnUtc == null && message.DeadLetteredOnUtc == null)
                .MinAsync(message => (DateTime?)message.CreatedOnUtc, cancellationToken)
                .ConfigureAwait(false);
            InfrastructureTelemetry.RecordOutboxOldestPendingAge(outboxName, observedAtUtc, oldestCreatedOnUtc);
            return processed;
        } finally {
            stopwatch.Stop();
            InfrastructureTelemetry.RecordOutboxProcessingDuration(outboxName, stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    private static async Task<bool> ProcessClaimedMessageAsync<TMessage>(
        FoodDiaryDbContext context,
        TMessage message,
        string outboxName,
        OutboxProcessingOptions options,
        TimeProvider timeProvider,
        Func<TMessage, CancellationToken, Task> dispatchAsync,
        Func<TMessage, object?> messageIdentity,
        ILogger logger,
        Func<TMessage, DateTime, CancellationToken, Task<bool>>? tryMarkProcessedAsync,
        CancellationToken cancellationToken)
        where TMessage : class, IOutboxMessage {
        string outcome;
        try {
            using var dispatchTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            dispatchTimeout.CancelAfter(options.DispatchTimeout);
            await dispatchAsync(message, dispatchTimeout.Token).ConfigureAwait(false);
            DateTime processedOnUtc = timeProvider.GetUtcNow().UtcDateTime;
            bool markedProcessed = tryMarkProcessedAsync is null
                ? MarkProcessed(message, processedOnUtc)
                : await tryMarkProcessedAsync(message, processedOnUtc, dispatchTimeout.Token).ConfigureAwait(false);
            outcome = markedProcessed ? "processed" : "requeued";
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        } catch (OperationCanceledException ex) {
            InfrastructureTelemetry.RecordOutboxMessages(outboxName, "dispatch_timeout", 1);
            outcome = HandleFailure(
                message,
                new TimeoutException("Outbox dispatch exceeded its configured time budget.", ex),
                outboxName,
                messageIdentity,
                timeProvider,
                logger);
        } catch (Exception ex) {
            outcome = HandleFailure(message, ex, outboxName, messageIdentity, timeProvider, logger);
        }

        using var finalizationTimeout = new CancellationTokenSource(options.FinalizationTimeout);
        await context.SaveChangesAsync(finalizationTimeout.Token).ConfigureAwait(false);
        RecordOutcome(outboxName, outcome);
        context.ChangeTracker.Clear();
        return string.Equals(outcome, "processed", StringComparison.Ordinal);
    }

    private static void RecordOutcome(string outboxName, string outcome) {
        switch (outcome) {
            case "processed":
                InfrastructureTelemetry.RecordOutboxMessages(outboxName, "processed", 1);
                break;
            case "retried":
                InfrastructureTelemetry.RecordOutboxMessages(outboxName, "retried", 1);
                break;
            case "dead_lettered":
                InfrastructureTelemetry.RecordOutboxMessages(outboxName, "dead_lettered", 1);
                break;
            case "requeued":
                InfrastructureTelemetry.RecordOutboxMessages(outboxName, "requeued", 1);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unsupported outbox processing outcome.");
        }
    }

    private static bool MarkProcessed<TMessage>(TMessage message, DateTime processedOnUtc)
        where TMessage : IOutboxMessage {
        message.MarkProcessed(processedOnUtc);
        return true;
    }

    private static string HandleFailure<TMessage>(
        TMessage message,
        Exception exception,
        string outboxName,
        Func<TMessage, object?> messageIdentity,
        TimeProvider timeProvider,
        ILogger logger)
        where TMessage : IOutboxMessage {
        int attemptCount = message.AttemptCount + 1;
        string error = OutboxProcessingPolicy.FormatSafeError(exception);
        DateTime failedOnUtc = timeProvider.GetUtcNow().UtcDateTime;
        if (OutboxProcessingPolicy.ShouldDeadLetter(attemptCount)) {
            message.MarkDeadLettered(error, failedOnUtc);
            logger.LogError(
                "{OutboxName} outbox dead-lettered {MessageIdentity} after {AttemptCount} attempts. ErrorType={ErrorType}",
                outboxName,
                messageIdentity(message),
                message.AttemptCount,
                exception.GetType().Name);
            return "dead_lettered";
        }

        message.MarkFailed(error, failedOnUtc.Add(OutboxProcessingPolicy.CalculateRetryDelay(attemptCount)));
        logger.LogWarning(
            "{OutboxName} outbox failed for {MessageIdentity}. Attempt {AttemptCount} of {MaxAttemptCount}. ErrorType={ErrorType}",
            outboxName,
            messageIdentity(message),
            message.AttemptCount,
            OutboxProcessingPolicy.MaxAttemptCount,
            exception.GetType().Name);
        return "retried";
    }
}
