using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Achievements;

internal sealed class AchievementEvaluationOutbox(
    FoodDiaryDbContext context,
    TimeProvider timeProvider) : IAchievementEvaluationOutbox {
    public async Task EnqueueAsync(UserId userId, CancellationToken cancellationToken = default) {
        DateTime requestedOnUtc = timeProvider.GetUtcNow().UtcDateTime;
        if (!context.Database.IsRelational()) {
            AchievementEvaluationOutboxMessage? existing = await context.AchievementEvaluationOutbox
                .SingleOrDefaultAsync(message => message.UserId == userId, cancellationToken)
                .ConfigureAwait(false);
            if (existing is null) {
                await context.AchievementEvaluationOutbox
                    .AddAsync(AchievementEvaluationOutboxMessage.Create(userId, requestedOnUtc), cancellationToken)
                    .ConfigureAwait(false);
            } else {
                existing.RequestEvaluation(requestedOnUtc);
            }

            return;
        }

        var message = AchievementEvaluationOutboxMessage.Create(userId, requestedOnUtc);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "AchievementEvaluationOutbox"
                ("Id", "UserId", "CreatedOnUtc", "NextAttemptOnUtc", "AttemptCount", "Revision")
            VALUES
                ({message.Id}, {userId.Value}, {requestedOnUtc}, {requestedOnUtc}, 0, 1)
            ON CONFLICT ("UserId") DO UPDATE
            SET "Revision" = "AchievementEvaluationOutbox"."Revision" + 1,
                "CreatedOnUtc" = {requestedOnUtc},
                "NextAttemptOnUtc" = {requestedOnUtc},
                "AttemptCount" = 0,
                "ProcessedOnUtc" = NULL,
                "DeadLetteredOnUtc" = NULL,
                "LastError" = NULL
            """,
            cancellationToken).ConfigureAwait(false);
    }
}
