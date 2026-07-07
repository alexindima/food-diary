using System.Diagnostics;
using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Infrastructure.Persistence.Outbox;
using FoodDiary.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Infrastructure.Persistence.Email;

internal sealed class EmailOutboxProcessor(
    FoodDiaryDbContext context,
    IEmailTransport emailTransport,
    TimeProvider timeProvider,
    ILogger<EmailOutboxProcessor> logger) : IEmailOutboxProcessor {
    private const string OutboxName = "email";

    public async Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default) {
        if (batchSize <= 0) {
            return 0;
        }

        var stopwatch = Stopwatch.StartNew();
        try {
            DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;
            List<EmailOutboxMessage> messages = await OutboxMessageClaimer
                .ClaimDueAsync(context, context.EmailOutbox, "\"EmailOutbox\"", batchSize, nowUtc, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            InfrastructureTelemetry.RecordOutboxMessages(OutboxName, "claimed", messages.Count);

            int processed = 0;
            int retried = 0;
            int deadLettered = 0;
            foreach (EmailOutboxMessage message in messages) {
                try {
                    await emailTransport.SendAsync(message.ToEmailMessage(), cancellationToken).ConfigureAwait(false);
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

    private bool HandleFailure(EmailOutboxMessage message, Exception ex) {
        int attemptCount = message.AttemptCount + 1;
        string error = OutboxProcessingPolicy.TruncateError(ex.ToString());
        DateTime failedOnUtc = timeProvider.GetUtcNow().UtcDateTime;
        if (OutboxProcessingPolicy.ShouldDeadLetter(attemptCount)) {
            message.MarkDeadLettered(error, failedOnUtc);
            logger.LogError(ex, "Email outbox dead-lettered {EmailOutboxMessageId} after {AttemptCount} attempts.", message.Id, message.AttemptCount);
            return true;
        }

        TimeSpan retryDelay = OutboxProcessingPolicy.CalculateRetryDelay(attemptCount);
        message.MarkFailed(error, failedOnUtc.Add(retryDelay));
        logger.LogWarning(
            ex,
            "Email outbox failed for {EmailOutboxMessageId}. Attempt {AttemptCount} of {MaxAttemptCount}.",
            message.Id,
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