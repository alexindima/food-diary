using FoodDiary.Application.Abstractions.Common.Abstractions.Outbox;
using FoodDiary.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Gamification.Infrastructure.Persistence;

internal sealed class AchievementEvaluationOutboxReplayStream(FoodDiaryDbContext context) : IOutboxReplayStream {
    public string Name => "achievement_evaluation";
    public int Order => 3;
    public string? ReplayRejectionReason => null;

    public async Task<OutboxReplayEntry?> FindAsync(Guid messageId, bool forUpdate, CancellationToken cancellationToken = default) {
        AchievementEvaluationOutboxMessage? message = await (forUpdate
            ? context.AchievementEvaluationOutbox.FromSqlInterpolated($"SELECT * FROM \"AchievementEvaluationOutbox\" WHERE \"Id\" = {messageId} FOR UPDATE")
            : context.AchievementEvaluationOutbox)
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return message is null ? null : new OutboxReplayEntry(message, message.LastError, message.UserId.Value.ToString());
    }

    public async Task<IReadOnlyList<OutboxDeadLetterMessageModel>> ListAsync(
        int limit,
        CancellationToken cancellationToken = default) =>
        await context.AchievementEvaluationOutbox
            .AsNoTracking()
            .Where(message => message.DeadLetteredOnUtc != null && message.ProcessedOnUtc == null)
            .OrderByDescending(message => message.DeadLetteredOnUtc)
            .Take(limit)
            .Select(message => new OutboxDeadLetterMessageModel(
                "achievement_evaluation",
                message.Id,
                message.CreatedOnUtc,
                message.DeadLetteredOnUtc!.Value,
                message.AttemptCount,
                message.LastError,
                message.UserId.Value.ToString()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
