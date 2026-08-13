using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Application.Gamification.Common;
using FoodDiary.Application.Gamification.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Gamification.Services;

public sealed class AchievementReconciliationHandler(
    IMealActivityReadService mealActivityReadService,
    IAchievementMetricReader achievementMetricReader,
    IAchievementAwardService achievementAwardService,
    TimeProvider timeProvider) : IAchievementReconciliationHandler {
    public async Task ReconcileAsync(UserId userId, DateTime occurredAtUtc, CancellationToken cancellationToken = default) {
        DateTime today = timeProvider.GetUtcNow().UtcDateTime.Date;
        IReadOnlyList<DateTime> mealDates = await mealActivityReadService
            .GetDistinctMealDatesAsync(userId, DateTime.UnixEpoch, today, cancellationToken)
            .ConfigureAwait(false);
        (_, int longestStreak) = GamificationCalculator.CalculateStreaks(mealDates, today);
        int totalMeals = await mealActivityReadService.GetTotalMealCountAsync(userId, cancellationToken).ConfigureAwait(false);
        int totalAcademyArticlesRead = await achievementMetricReader
            .GetCompletedAcademyArticleCountAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        var metrics = new AchievementMetricSnapshot(longestStreak, totalMeals, totalAcademyArticlesRead);
        await achievementAwardService.EvaluateAndGrantAsync(userId, metrics, cancellationToken, occurredAtUtc).ConfigureAwait(false);
    }
}
