using FoodDiary.Application.Abstractions.Common.Abstractions.Outbox;
using FoodDiary.Infrastructure.Persistence.Email;
using FoodDiary.Infrastructure.Persistence.Images;
using FoodDiary.Infrastructure.Persistence.Notifications;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FoodDiary.Infrastructure.Persistence.Outbox;

internal sealed class OutboxDeadLetterReplayService(
    FoodDiaryDbContext context,
    TimeProvider timeProvider) : IOutboxDeadLetterReplayService {
    private const int MaximumListLimit = 200;

    public async Task<IReadOnlyList<OutboxDeadLetterMessageModel>> ListDeadLettersAsync(
        string? outboxName,
        int limit,
        CancellationToken cancellationToken = default) {
        int boundedLimit = ValidateLimit(limit);
        string? normalizedName = NormalizeOptionalOutboxName(outboxName);
        var result = new List<OutboxDeadLetterMessageModel>();

        if (normalizedName is null or "email") {
            result.AddRange(await ListEmailAsync(boundedLimit, cancellationToken).ConfigureAwait(false));
        }
        if (normalizedName is null or "image_object_deletion") {
            result.AddRange(await ListImageDeletionAsync(boundedLimit, cancellationToken).ConfigureAwait(false));
        }
        if (normalizedName is null or "notification_web_push") {
            result.AddRange(await ListWebPushAsync(boundedLimit, cancellationToken).ConfigureAwait(false));
        }

        return [.. result
            .OrderByDescending(static message => message.DeadLetteredOnUtc)
            .Take(boundedLimit)];
    }

    public async Task<OutboxDeadLetterMessageModel?> GetDeadLetterAsync(
        string outboxName,
        Guid messageId,
        CancellationToken cancellationToken = default) {
        ValidateMessageId(messageId);
        IOutboxMessage? message = await FindAsync(
            NormalizeOutboxName(outboxName),
            messageId,
            forUpdate: false,
            cancellationToken).ConfigureAwait(false);
        return message?.DeadLetteredOnUtc is null ? null : ToModel(NormalizeOutboxName(outboxName), message);
    }

    public async Task<IReadOnlyList<OutboxReplayAuditModel>> ListReplayHistoryAsync(
        string? outboxName,
        Guid? messageId,
        int limit,
        CancellationToken cancellationToken = default) {
        int boundedLimit = ValidateLimit(limit);
        string? normalizedName = NormalizeOptionalOutboxName(outboxName);
        IQueryable<OutboxReplayAudit> query = context.OutboxReplayAudits.AsNoTracking();
        if (normalizedName is not null) {
            query = query.Where(entry => entry.OutboxName == normalizedName);
        }
        if (messageId.HasValue) {
            ValidateMessageId(messageId.Value);
            query = query.Where(entry => entry.MessageId == messageId.Value);
        }

        return await query
            .OrderByDescending(entry => entry.RequestedOnUtc)
            .Take(boundedLimit)
            .Select(entry => new OutboxReplayAuditModel(
                entry.Id,
                entry.OutboxName,
                entry.MessageId,
                entry.RequestedBy,
                entry.Reason,
                entry.RequestedOnUtc,
                entry.PreviousAttemptCount,
                entry.PreviousError))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<OutboxReplayAuditModel> ReplayAsync(
        string outboxName,
        Guid messageId,
        string requestedBy,
        string reason,
        int expectedAttemptCount,
        CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ValidateMessageId(messageId);
        if (expectedAttemptCount <= 0) {
            throw new ArgumentOutOfRangeException(nameof(expectedAttemptCount), "Expected attempt count must be positive.");
        }

        string normalizedName = NormalizeOutboxName(outboxName);
        IDbContextTransaction? transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false)
            : null;
        try {
            IOutboxMessage message = await FindAsync(
                normalizedName,
                messageId,
                forUpdate: transaction is not null,
                cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Outbox message was not found.");
            if (message.DeadLetteredOnUtc is null || message.ProcessedOnUtc is not null) {
                throw new InvalidOperationException("Only a dead-lettered, unprocessed message can be replayed.");
            }
            if (message.AttemptCount != expectedAttemptCount) {
                throw new InvalidOperationException(
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Outbox message changed after inspection. Expected {expectedAttemptCount} attempts, observed {message.AttemptCount}."));
            }

            DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;
            var audit = OutboxReplayAudit.Create(
                normalizedName,
                messageId,
                requestedBy,
                reason,
                nowUtc,
                message.AttemptCount,
                GetLastError(message));
            context.OutboxReplayAudits.Add(audit);
            message.MarkReplayed(nowUtc);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            if (transaction is not null) {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            return ToModel(audit);
        } finally {
            if (transaction is not null) {
                await transaction.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private async Task<IOutboxMessage?> FindAsync(
        string outboxName,
        Guid messageId,
        bool forUpdate,
        CancellationToken cancellationToken) =>
        outboxName switch {
            "email" => await FindEmailAsync(messageId, forUpdate, cancellationToken).ConfigureAwait(false),
            "image_object_deletion" => await FindImageDeletionAsync(messageId, forUpdate, cancellationToken).ConfigureAwait(false),
            "notification_web_push" => await FindWebPushAsync(messageId, forUpdate, cancellationToken).ConfigureAwait(false),
            _ => throw new ArgumentOutOfRangeException(nameof(outboxName), "Unsupported outbox name."),
        };

    private Task<EmailOutboxMessage?> FindEmailAsync(Guid messageId, bool forUpdate, CancellationToken cancellationToken) =>
        (forUpdate
            ? context.EmailOutbox.FromSqlInterpolated($"SELECT * FROM \"EmailOutbox\" WHERE \"Id\" = {messageId} FOR UPDATE")
            : context.EmailOutbox)
        .SingleOrDefaultAsync(cancellationToken);

    private Task<ImageObjectDeletionOutboxMessage?> FindImageDeletionAsync(
        Guid messageId,
        bool forUpdate,
        CancellationToken cancellationToken) =>
        (forUpdate
            ? context.ImageObjectDeletionOutbox.FromSqlInterpolated($"SELECT * FROM \"ImageObjectDeletionOutbox\" WHERE \"Id\" = {messageId} FOR UPDATE")
            : context.ImageObjectDeletionOutbox)
        .SingleOrDefaultAsync(cancellationToken);

    private Task<NotificationWebPushOutboxMessage?> FindWebPushAsync(
        Guid messageId,
        bool forUpdate,
        CancellationToken cancellationToken) =>
        (forUpdate
            ? context.NotificationWebPushOutbox.FromSqlInterpolated($"SELECT * FROM \"NotificationWebPushOutbox\" WHERE \"Id\" = {messageId} FOR UPDATE")
            : context.NotificationWebPushOutbox)
        .SingleOrDefaultAsync(cancellationToken);

    private async Task<IReadOnlyList<OutboxDeadLetterMessageModel>> ListEmailAsync(
        int limit,
        CancellationToken cancellationToken) =>
        await context.EmailOutbox
            .AsNoTracking()
            .Where(message => message.DeadLetteredOnUtc != null && message.ProcessedOnUtc == null)
            .OrderByDescending(message => message.DeadLetteredOnUtc)
            .Take(limit)
            .Select(message => new OutboxDeadLetterMessageModel(
                "email",
                message.Id,
                message.CreatedOnUtc,
                message.DeadLetteredOnUtc!.Value,
                message.AttemptCount,
                message.LastError,
                message.Subject))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    private async Task<IReadOnlyList<OutboxDeadLetterMessageModel>> ListImageDeletionAsync(
        int limit,
        CancellationToken cancellationToken) =>
        await context.ImageObjectDeletionOutbox
            .AsNoTracking()
            .Where(message => message.DeadLetteredOnUtc != null && message.ProcessedOnUtc == null)
            .OrderByDescending(message => message.DeadLetteredOnUtc)
            .Take(limit)
            .Select(message => new OutboxDeadLetterMessageModel(
                "image_object_deletion",
                message.Id,
                message.CreatedOnUtc,
                message.DeadLetteredOnUtc!.Value,
                message.AttemptCount,
                message.LastError,
                message.ObjectKey))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    private async Task<IReadOnlyList<OutboxDeadLetterMessageModel>> ListWebPushAsync(
        int limit,
        CancellationToken cancellationToken) =>
        await context.NotificationWebPushOutbox
            .AsNoTracking()
            .Where(message => message.DeadLetteredOnUtc != null && message.ProcessedOnUtc == null)
            .OrderByDescending(message => message.DeadLetteredOnUtc)
            .Take(limit)
            .Select(message => new OutboxDeadLetterMessageModel(
                "notification_web_push",
                message.Id,
                message.CreatedOnUtc,
                message.DeadLetteredOnUtc!.Value,
                message.AttemptCount,
                message.LastError,
                message.NotificationId.Value.ToString()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    private static OutboxDeadLetterMessageModel ToModel(string outboxName, IOutboxMessage message) =>
        message switch {
            EmailOutboxMessage email => new(
                outboxName,
                email.Id,
                email.CreatedOnUtc,
                email.DeadLetteredOnUtc!.Value,
                email.AttemptCount,
                email.LastError,
                email.Subject),
            ImageObjectDeletionOutboxMessage image => new(
                outboxName,
                image.Id,
                image.CreatedOnUtc,
                image.DeadLetteredOnUtc!.Value,
                image.AttemptCount,
                image.LastError,
                image.ObjectKey),
            NotificationWebPushOutboxMessage notification => new(
                outboxName,
                notification.Id,
                notification.CreatedOnUtc,
                notification.DeadLetteredOnUtc!.Value,
                notification.AttemptCount,
                notification.LastError,
                notification.NotificationId.Value.ToString()),
            _ => throw new ArgumentOutOfRangeException(nameof(message)),
        };

    private static OutboxReplayAuditModel ToModel(OutboxReplayAudit audit) =>
        new(
            audit.Id,
            audit.OutboxName,
            audit.MessageId,
            audit.RequestedBy,
            audit.Reason,
            audit.RequestedOnUtc,
            audit.PreviousAttemptCount,
            audit.PreviousError);

    private static int ValidateLimit(int limit) {
        if (limit is <= 0 or > MaximumListLimit) {
            throw new ArgumentOutOfRangeException(nameof(limit), $"Limit must be between 1 and {MaximumListLimit}.");
        }

        return limit;
    }

    private static void ValidateMessageId(Guid messageId) {
        if (messageId == Guid.Empty) {
            throw new ArgumentException("Message id is required.", nameof(messageId));
        }
    }

    private static string? NormalizeOptionalOutboxName(string? outboxName) =>
        string.IsNullOrWhiteSpace(outboxName) ? null : NormalizeOutboxName(outboxName);

    private static string NormalizeOutboxName(string outboxName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxName);
        string normalized = outboxName.Trim().ToLowerInvariant();
        return normalized is "email" or "image_object_deletion" or "notification_web_push"
            ? normalized
            : throw new ArgumentOutOfRangeException(nameof(outboxName), "Unsupported outbox name.");
    }

    private static string? GetLastError(IOutboxMessage message) =>
        message switch {
            Email.EmailOutboxMessage email => email.LastError,
            Images.ImageObjectDeletionOutboxMessage image => image.LastError,
            Notifications.NotificationWebPushOutboxMessage notification => notification.LastError,
            _ => null,
        };
}
