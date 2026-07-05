using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Infrastructure.Persistence.Outbox;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Infrastructure.Persistence.Email;

internal sealed class EmailOutboxProcessor(
    FoodDiaryDbContext context,
    IEmailTransport emailTransport,
    TimeProvider timeProvider,
    ILogger<EmailOutboxProcessor> logger) : IEmailOutboxProcessor {
    private const int MaxErrorLength = 2048;

    public async Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default) {
        if (batchSize <= 0) {
            return 0;
        }

        DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        List<EmailOutboxMessage> messages = await OutboxMessageClaimer
            .ClaimDueAsync(
                context,
                context.EmailOutbox,
                "\"EmailOutbox\"",
                batchSize,
                nowUtc,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        int processed = 0;
        foreach (EmailOutboxMessage message in messages) {
            try {
                await emailTransport.SendAsync(message.ToEmailMessage(), cancellationToken).ConfigureAwait(false);
                message.MarkProcessed(timeProvider.GetUtcNow().UtcDateTime);
                processed++;
            } catch (Exception ex) {
                TimeSpan retryDelay = CalculateRetryDelay(message.AttemptCount + 1);
                message.MarkFailed(TruncateError(ex.ToString()), timeProvider.GetUtcNow().UtcDateTime.Add(retryDelay));
                logger.LogWarning(
                    ex,
                    "Email outbox failed for {EmailOutboxMessageId}. Attempt {AttemptCount}.",
                    message.Id,
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
