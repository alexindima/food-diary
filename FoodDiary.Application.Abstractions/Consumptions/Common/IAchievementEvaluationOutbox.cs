using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Consumptions.Common;

public interface IAchievementEvaluationOutbox {
    Task EnqueueAsync(UserId userId, CancellationToken cancellationToken = default);
}
