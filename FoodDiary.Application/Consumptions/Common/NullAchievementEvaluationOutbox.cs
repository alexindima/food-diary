using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Common;

internal sealed class NullAchievementEvaluationOutbox : IAchievementEvaluationOutbox {
    public static readonly NullAchievementEvaluationOutbox Instance = new();

    private NullAchievementEvaluationOutbox() {
    }

    public Task EnqueueAsync(UserId userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
