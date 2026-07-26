using FoodDiary.Application.Abstractions.Common.Abstractions.Outbox;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Outbox;

internal sealed class OutboxDeadLetterReplayService(
    FoodDiaryDbContext context,
    TimeProvider timeProvider) : IOutboxDeadLetterReplayService {
    public async Task ReplayAsync(
        string outboxName,
        Guid messageId,
        string requestedBy,
        string reason,
        CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxName);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (messageId == Guid.Empty) {
            throw new ArgumentException("Message id is required.", nameof(messageId));
        }

        IOutboxMessage message = await FindAsync(outboxName.Trim(), messageId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Outbox message was not found.");
        if (message.DeadLetteredOnUtc is null || message.ProcessedOnUtc is not null) {
            throw new InvalidOperationException("Only a dead-lettered, unprocessed message can be replayed.");
        }

        DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        context.OutboxReplayAudits.Add(OutboxReplayAudit.Create(
            outboxName.Trim(),
            messageId,
            requestedBy,
            reason,
            nowUtc,
            message.AttemptCount,
            GetLastError(message)));
        message.MarkReplayed(nowUtc);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<IOutboxMessage?> FindAsync(
        string outboxName,
        Guid messageId,
        CancellationToken cancellationToken) =>
        outboxName.ToLowerInvariant() switch {
            "email" => await context.EmailOutbox.SingleOrDefaultAsync(message => message.Id == messageId, cancellationToken).ConfigureAwait(false),
            "image_object_deletion" => await context.ImageObjectDeletionOutbox.SingleOrDefaultAsync(message => message.Id == messageId, cancellationToken).ConfigureAwait(false),
            "notification_web_push" => await context.NotificationWebPushOutbox.SingleOrDefaultAsync(message => message.Id == messageId, cancellationToken).ConfigureAwait(false),
            _ => throw new ArgumentOutOfRangeException(nameof(outboxName), "Unsupported outbox name."),
        };

    private static string? GetLastError(IOutboxMessage message) =>
        message switch {
            Email.EmailOutboxMessage email => email.LastError,
            Images.ImageObjectDeletionOutboxMessage image => image.LastError,
            Notifications.NotificationWebPushOutboxMessage notification => notification.LastError,
            _ => null,
        };
}
