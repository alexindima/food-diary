using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Gamification.Application.Abstractions.Achievements.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ReadModel.Composition.Gamification;

public sealed class AchievementMetricReader(ICompositionReadContext context) : IAchievementMetricReader {
    public Task<int> GetCompletedAcademyArticleCountAsync(
        UserId userId,
        CancellationToken cancellationToken = default) =>
        context.UserLessonProgress
            .AsNoTracking()
            .CountAsync(progress => progress.UserId == userId, cancellationToken);
}
