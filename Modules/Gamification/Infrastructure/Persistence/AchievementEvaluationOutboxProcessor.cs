using FoodDiary.Outbox.Infrastructure.Options;
using FoodDiary.Outbox.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.Achievements.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Gamification.Infrastructure.Persistence;

internal sealed class AchievementEvaluationOutboxProcessor(
    DbContext context,
    DbSet<AchievementEvaluationOutboxMessage> messages,
    IAchievementReconciliationHandler reconciliationHandler,
    IOptions<OutboxProcessingOptions> options,
    TimeProvider timeProvider,
    ILogger<AchievementEvaluationOutboxProcessor> logger, Action? ensureCleanEntry = null) : IAchievementEvaluationOutboxProcessor {
    public Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default) =>
        OutboxProcessingEngine.ProcessDueAsync(
            context,
            messages,
            "\"AchievementEvaluationOutbox\"",
            "achievement_evaluation",
            batchSize,
            options.Value,
            timeProvider,
            (message, token) => reconciliationHandler.ReconcileAsync(message.UserId, message.CreatedOnUtc, token),
            static message => message.UserId.Value,
            logger,
            cancellationToken: cancellationToken,
            tryReleaseUpdatedRevisionAsync: TryReleaseUpdatedRevisionAsync,
            ensureCleanEntry: ensureCleanEntry);

    private async Task<OutboxCompletionResult> TryReleaseUpdatedRevisionAsync(
        AchievementEvaluationOutboxMessage message,
        CancellationToken cancellationToken) {
        // Finalization clears the current lease; fencing must use the originally claimed values.
        long claimedRevision = context.Entry(message).OriginalValues.GetValue<long>(nameof(message.Revision));
        string? claimedBy = context.Entry(message).OriginalValues.GetValue<string?>(nameof(message.LockedBy));
        context.ChangeTracker.Clear();
        if (!context.Database.IsRelational()) {
            AchievementEvaluationOutboxMessage? current = await messages
                .SingleOrDefaultAsync(candidate => candidate.Id == message.Id, cancellationToken).ConfigureAwait(false);
            if (current is null || current.Revision == claimedRevision ||
                !string.Equals(current.LockedBy, claimedBy, StringComparison.Ordinal)) {
                return OutboxCompletionResult.ClaimLost;
            }
            current.ReleaseForUpdatedRevision();
            return OutboxCompletionResult.Requeued;
        }

#pragma warning disable MA0076
        int released = await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE "AchievementEvaluationOutbox"
            SET "LockedUntilUtc" = NULL, "LockedBy" = NULL
            WHERE "Id" = {message.Id} AND "LockedBy" = {claimedBy} AND "Revision" <> {claimedRevision}
            """,
            cancellationToken: cancellationToken).ConfigureAwait(false);
#pragma warning restore MA0076
        return released == 1 ? OutboxCompletionResult.Requeued : OutboxCompletionResult.ClaimLost;
    }
}
