using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Gamification.Infrastructure.Persistence;

internal sealed class AchievementEvaluationOutboxProcessor(
    FoodDiaryDbContext context,
    IAchievementReconciliationHandler reconciliationHandler,
    IOptions<OutboxProcessingOptions> options,
    TimeProvider timeProvider,
    ILogger<AchievementEvaluationOutboxProcessor> logger) : IAchievementEvaluationOutboxProcessor {
    public Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default) =>
        OutboxProcessingEngine.ProcessDueAsync(
            context,
            context.AchievementEvaluationOutbox,
            "\"AchievementEvaluationOutbox\"",
            "achievement_evaluation",
            batchSize,
            options.Value,
            timeProvider,
            (message, token) => reconciliationHandler.ReconcileAsync(message.UserId, message.CreatedOnUtc, token),
            static message => message.UserId.Value,
            logger,
            cancellationToken: cancellationToken,
            tryMarkProcessedAsync: TryMarkProcessedAsync);

    private async Task<OutboxCompletionResult> TryMarkProcessedAsync(
        AchievementEvaluationOutboxMessage message,
        DateTime processedOnUtc,
        CancellationToken cancellationToken) {
        long claimedRevision = message.Revision;
        string? claimedBy = message.LockedBy;
        if (!context.Database.IsRelational()) {
            await context.Entry(message).ReloadAsync(cancellationToken).ConfigureAwait(false);
            if (!string.Equals(message.LockedBy, claimedBy, StringComparison.Ordinal)) {
                return OutboxCompletionResult.ClaimLost;
            }
            if (message.Revision != claimedRevision) {
                message.ReleaseForUpdatedRevision();
                return OutboxCompletionResult.Requeued;
            }

            message.MarkProcessed(processedOnUtc);
            return OutboxCompletionResult.Processed;
        }

#pragma warning disable MA0076
        int completed = await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE "AchievementEvaluationOutbox"
            SET "ProcessedOnUtc" = {processedOnUtc},
                "LockedUntilUtc" = NULL,
                "LockedBy" = NULL,
                "LastError" = NULL
            WHERE "Id" = {message.Id} AND "Revision" = {claimedRevision} AND "LockedBy" = {claimedBy}
            """,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (completed == 1) {
            return OutboxCompletionResult.Processed;
        }

        int released = await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE "AchievementEvaluationOutbox"
            SET "LockedUntilUtc" = NULL, "LockedBy" = NULL
            WHERE "Id" = {message.Id} AND "LockedBy" = {claimedBy}
            """,
            cancellationToken: cancellationToken).ConfigureAwait(false);
#pragma warning restore MA0076
        return released == 1 ? OutboxCompletionResult.Requeued : OutboxCompletionResult.ClaimLost;
    }
}
