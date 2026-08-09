using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Achievements;

internal sealed class AchievementMetricReader(FoodDiaryDbContext context) : IAchievementMetricReader {
    public Task<int> GetCompletedAcademyArticleCountAsync(
        UserId userId,
        CancellationToken cancellationToken = default) =>
        context.Set<UserLessonProgress>()
            .AsNoTracking()
            .CountAsync(progress => progress.UserId == userId, cancellationToken);
}
