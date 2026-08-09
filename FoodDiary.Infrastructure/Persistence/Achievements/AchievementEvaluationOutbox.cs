using FoodDiary.Application.Abstractions.Consumptions.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Persistence.Achievements;

internal sealed class AchievementEvaluationOutbox(
    FoodDiaryDbContext context,
    TimeProvider timeProvider) : IAchievementEvaluationOutbox {
    public async Task EnqueueAsync(UserId userId, CancellationToken cancellationToken = default) {
        var message = AchievementEvaluationOutboxMessage.Create(userId, timeProvider.GetUtcNow().UtcDateTime);
        await context.AchievementEvaluationOutbox.AddAsync(message, cancellationToken).ConfigureAwait(false);
    }
}
