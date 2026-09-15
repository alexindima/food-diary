using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Meals.Contracts.Common;

public interface IMealAchievementEvaluationRequest {
    Task EnqueueAsync(UserId userId, CancellationToken cancellationToken = default);
}
