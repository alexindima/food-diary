using FoodDiary.Application.Abstractions.Common.Abstractions.Outbox;
using FoodDiary.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Email;

internal sealed class EmailOutboxReplayStream(FoodDiaryDbContext context) : IOutboxReplayStream {
    public string Name => "email";
    public int Order => 0;
    public string? ReplayRejectionReason => "Dead-lettered emails cannot be replayed because their sensitive payloads are removed. Regenerate the email through its originating workflow.";

    public async Task<OutboxReplayEntry?> FindAsync(Guid messageId, bool forUpdate, CancellationToken cancellationToken = default) {
        EmailOutboxMessage? message = await (forUpdate
            ? context.EmailOutbox.FromSqlInterpolated($"SELECT * FROM \"EmailOutbox\" WHERE \"Id\" = {messageId} FOR UPDATE")
            : context.EmailOutbox.Where(message => message.Id == messageId))
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return message is null ? null : new OutboxReplayEntry(message, message.LastError, message.Subject);
    }

    public async Task<IReadOnlyList<OutboxDeadLetterMessageModel>> ListAsync(
        int limit,
        CancellationToken cancellationToken = default) =>
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
}
