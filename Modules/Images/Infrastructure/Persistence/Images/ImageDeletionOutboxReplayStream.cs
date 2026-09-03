using FoodDiary.Application.Abstractions.Common.Abstractions.Outbox;
using FoodDiary.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Images;

internal sealed class ImageDeletionOutboxReplayStream(FoodDiaryDbContext context) : IOutboxReplayStream {
    public string Name => "image_object_deletion";
    public int Order => 1;
    public string? ReplayRejectionReason => null;

    public async Task<OutboxReplayEntry?> FindAsync(Guid messageId, bool forUpdate, CancellationToken cancellationToken = default) {
        ImageObjectDeletionOutboxMessage? message = await (forUpdate
            ? context.ImageObjectDeletionOutbox.FromSqlInterpolated($"SELECT * FROM \"ImageObjectDeletionOutbox\" WHERE \"Id\" = {messageId} FOR UPDATE")
            : context.ImageObjectDeletionOutbox)
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return message is null ? null : new OutboxReplayEntry(message, message.LastError, message.ObjectKey);
    }

    public async Task<IReadOnlyList<OutboxDeadLetterMessageModel>> ListAsync(
        int limit,
        CancellationToken cancellationToken = default) =>
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
}
