using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Gamification.Application.Abstractions.Achievements.Common;

public interface IAchievementMetricReader {
    Task<int> GetCompletedAcademyArticleCountAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}
