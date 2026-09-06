using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Meals.Common;

internal sealed class NullAchievementEvaluationOutbox : IMealAchievementEvaluationRequest {
    public static readonly NullAchievementEvaluationOutbox Instance = new();

    private NullAchievementEvaluationOutbox() {
    }

    public Task EnqueueAsync(UserId userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
