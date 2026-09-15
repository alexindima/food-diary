using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Gamification.Contracts.Achievements.Common;

public interface IAchievementEvaluationOutbox {
    Task EnqueueAsync(UserId userId, CancellationToken cancellationToken = default);
}
