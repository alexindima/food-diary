using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure.Persistence.Achievements;

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

    private async Task<bool> TryMarkProcessedAsync(
        AchievementEvaluationOutboxMessage message,
        DateTime processedOnUtc,
        CancellationToken cancellationToken) {
        long claimedRevision = message.Revision;
        if (!context.Database.IsRelational()) {
            await context.Entry(message).ReloadAsync(cancellationToken).ConfigureAwait(false);
            if (message.Revision != claimedRevision) {
                message.ReleaseForUpdatedRevision();
                return false;
            }

            message.MarkProcessed(processedOnUtc);
            return true;
        }

#pragma warning disable MA0076
        int completed = await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE "AchievementEvaluationOutbox"
            SET "ProcessedOnUtc" = {processedOnUtc},
                "LockedUntilUtc" = NULL,
                "LockedBy" = NULL,
                "LastError" = NULL
            WHERE "Id" = {message.Id} AND "Revision" = {claimedRevision}
            """,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (completed == 1) {
            return true;
        }

        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE "AchievementEvaluationOutbox"
            SET "LockedUntilUtc" = NULL, "LockedBy" = NULL
            WHERE "Id" = {message.Id}
            """,
            cancellationToken: cancellationToken).ConfigureAwait(false);
#pragma warning restore MA0076
        return false;
    }
}
