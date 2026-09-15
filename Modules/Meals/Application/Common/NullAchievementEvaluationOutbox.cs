using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Meals.Application.Common;

internal sealed class NullAchievementEvaluationOutbox : IMealAchievementEvaluationRequest {
    public static readonly NullAchievementEvaluationOutbox Instance = new();

    private NullAchievementEvaluationOutbox() {
    }

    public Task EnqueueAsync(UserId userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
