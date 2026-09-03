using FoodDiary.Application.Abstractions.Common.Abstractions.Outbox;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FoodDiary.Infrastructure.Persistence.Outbox;

internal sealed class OutboxDeadLetterReplayService(
    FoodDiaryDbContext context,
    TimeProvider timeProvider,
    IEnumerable<IOutboxReplayStream> streams) : IOutboxDeadLetterReplayService {
    private const int MaximumListLimit = 200;
    private readonly IReadOnlyDictionary<string, IOutboxReplayStream> _streams =
        streams.ToDictionary(static stream => stream.Name, StringComparer.Ordinal);

    public async Task<IReadOnlyList<OutboxDeadLetterMessageModel>> ListDeadLettersAsync(
        string? outboxName,
        int limit,
        CancellationToken cancellationToken = default) {
        int boundedLimit = ValidateLimit(limit);
        string? normalizedName = NormalizeOptionalOutboxName(outboxName);
        var result = new List<OutboxDeadLetterMessageModel>();

        foreach (IOutboxReplayStream stream in _streams.Values.OrderBy(static stream => stream.Order)) {
            if (normalizedName is null || string.Equals(normalizedName, stream.Name, StringComparison.Ordinal)) {
                result.AddRange(await stream.ListAsync(boundedLimit, cancellationToken).ConfigureAwait(false));
            }
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
        OutboxReplayEntry? message = await FindAsync(
            NormalizeOutboxName(outboxName),
            messageId,
            forUpdate: false,
            cancellationToken).ConfigureAwait(false);
        return message?.Message.DeadLetteredOnUtc is null ? null : ToModel(NormalizeOutboxName(outboxName), message);
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
        if (_streams[normalizedName].ReplayRejectionReason is { } rejectionReason) {
            throw new InvalidOperationException(rejectionReason);
        }
        IDbContextTransaction? transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false)
            : null;
        try {
            OutboxReplayEntry entry = await FindAsync(
                normalizedName,
                messageId,
                forUpdate: transaction is not null,
                cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Outbox message was not found.");
            IOutboxMessage message = entry.Message;
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
                entry.LastError);
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

    private Task<OutboxReplayEntry?> FindAsync(
        string outboxName,
        Guid messageId,
        bool forUpdate,
        CancellationToken cancellationToken) =>
        _streams[outboxName].FindAsync(messageId, forUpdate, cancellationToken);

    private static OutboxDeadLetterMessageModel ToModel(string outboxName, OutboxReplayEntry entry) =>
        new(outboxName, entry.Message.Id, entry.Message.CreatedOnUtc,
            entry.Message.DeadLetteredOnUtc!.Value, entry.Message.AttemptCount, entry.LastError, entry.Summary);

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

    private string? NormalizeOptionalOutboxName(string? outboxName) =>
        string.IsNullOrWhiteSpace(outboxName) ? null : NormalizeOutboxName(outboxName);

    private string NormalizeOutboxName(string outboxName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(outboxName);
        string normalized = outboxName.Trim().ToLowerInvariant();
        return _streams.ContainsKey(normalized)
            ? normalized
            : throw new ArgumentOutOfRangeException(nameof(outboxName), "Unsupported outbox name.");
    }
}
