using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Achievements.Common;

public interface IAchievementEvaluationOutbox {
    Task EnqueueAsync(UserId userId, CancellationToken cancellationToken = default);
}
