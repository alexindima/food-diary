using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Meals.Common;

public interface IMealAchievementEvaluationRequest {
    Task EnqueueAsync(UserId userId, CancellationToken cancellationToken = default);
}
