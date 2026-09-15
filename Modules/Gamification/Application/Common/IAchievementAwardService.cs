using FoodDiary.Modules.Gamification.Application.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Gamification.Application.Common;

public interface IAchievementAwardService {
    Task<IReadOnlyList<BadgeModel>> EvaluateAndGrantAsync(
        UserId userId,
        AchievementMetricSnapshot metrics,
        CancellationToken cancellationToken = default,
        DateTime? earnedAtUtc = null);
}
