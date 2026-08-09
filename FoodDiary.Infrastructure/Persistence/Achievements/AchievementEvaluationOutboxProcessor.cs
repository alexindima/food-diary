using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Infrastructure.Persistence.Outbox;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Infrastructure.Persistence.Achievements;

internal sealed class AchievementEvaluationOutboxProcessor(
    FoodDiaryDbContext context,
    IAchievementReconciliationHandler reconciliationHandler,
    TimeProvider timeProvider,
    ILogger<AchievementEvaluationOutboxProcessor> logger) : IAchievementEvaluationOutboxProcessor {
    public Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default) =>
        OutboxProcessingEngine.ProcessDueAsync(
            context,
            context.AchievementEvaluationOutbox,
            "\"AchievementEvaluationOutbox\"",
            "achievement_evaluation",
            batchSize,
            timeProvider,
            (message, token) => reconciliationHandler.ReconcileAsync(message.UserId, message.CreatedOnUtc, token),
            static message => message.UserId.Value,
            logger,
            cancellationToken: cancellationToken);
}
