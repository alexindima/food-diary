using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Gamification.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Gamification.Services;

public sealed class AchievementReconciliationHandler(
    IMealActivityReadService mealActivityReadService,
    IAchievementAwardService achievementAwardService,
    TimeProvider timeProvider) : IAchievementReconciliationHandler {
    public async Task ReconcileAsync(UserId userId, DateTime occurredAtUtc, CancellationToken cancellationToken = default) {
        DateTime today = timeProvider.GetUtcNow().UtcDateTime.Date;
        IReadOnlyList<DateTime> mealDates = await mealActivityReadService
            .GetDistinctMealDatesAsync(userId, DateTime.UnixEpoch, today, cancellationToken)
            .ConfigureAwait(false);
        (_, int longestStreak) = GamificationCalculator.CalculateStreaks(mealDates, today);
        int totalMeals = await mealActivityReadService.GetTotalMealCountAsync(userId, cancellationToken).ConfigureAwait(false);
        await achievementAwardService.EvaluateAndGrantAsync(userId, longestStreak, totalMeals, cancellationToken, occurredAtUtc).ConfigureAwait(false);
    }
}
