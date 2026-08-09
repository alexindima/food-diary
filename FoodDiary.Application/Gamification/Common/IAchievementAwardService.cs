using FoodDiary.Application.Gamification.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Gamification.Common;

public interface IAchievementAwardService {
    Task<IReadOnlyList<BadgeModel>> EvaluateAndGrantAsync(
        UserId userId,
        int longestStreak,
        int totalMeals,
        CancellationToken cancellationToken = default,
        DateTime? earnedAtUtc = null);
}
