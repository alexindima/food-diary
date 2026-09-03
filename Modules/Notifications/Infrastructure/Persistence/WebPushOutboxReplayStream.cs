using FoodDiary.Application.Abstractions.Common.Abstractions.Outbox;
using FoodDiary.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Notifications;

internal sealed class WebPushOutboxReplayStream(FoodDiaryDbContext context) : IOutboxReplayStream {
    public string Name => "notification_web_push";
    public int Order => 2;
    public string? ReplayRejectionReason => null;

    public async Task<OutboxReplayEntry?> FindAsync(Guid messageId, bool forUpdate, CancellationToken cancellationToken = default) {
        NotificationWebPushOutboxMessage? message = await (forUpdate
            ? context.NotificationWebPushOutbox.FromSqlInterpolated($"SELECT * FROM \"NotificationWebPushOutbox\" WHERE \"Id\" = {messageId} FOR UPDATE")
            : context.NotificationWebPushOutbox)
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return message is null ? null : new OutboxReplayEntry(message, message.LastError, message.NotificationId.Value.ToString());
    }

    public async Task<IReadOnlyList<OutboxDeadLetterMessageModel>> ListAsync(
        int limit,
        CancellationToken cancellationToken = default) =>
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
}
