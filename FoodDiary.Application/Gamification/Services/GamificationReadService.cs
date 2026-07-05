using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Gamification.Common;
using FoodDiary.Application.Gamification.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Gamification.Services;

public sealed class GamificationReadService(
    IMealReadRepository mealRepository,
    IDashboardStatisticsReadService statisticsReadService,
    IGamificationUserProfileService userProfileService,
    TimeProvider dateTimeProvider)
    : IGamificationReadService {
    public async Task<Result<GamificationModel>> GetAsync(UserId userId, CancellationToken cancellationToken) {
        Result<IGamificationUserProfile> userProfileResult =
            await userProfileService.GetAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userProfileResult.IsFailure) {
            return Result.Failure<GamificationModel>(userProfileResult.Error);
        }

        DateTime today = dateTimeProvider.GetUtcNow().UtcDateTime.Date;
        DateTime streakFrom = today.AddDays(-365);

        IReadOnlyList<DateTime> mealDates = await mealRepository.GetDistinctMealDatesAsync(userId, streakFrom, today, cancellationToken).ConfigureAwait(false);
        (int currentStreak, int longestStreak) = GamificationCalculator.CalculateStreaks(mealDates, today);

        int totalMeals = await mealRepository.GetTotalMealCountAsync(userId, cancellationToken).ConfigureAwait(false);

        DateTime weekStart = today.AddDays(-6);
        Result<IReadOnlyList<DashboardStatisticsBucketReadModel>> weeklyCaloriesResult = await statisticsReadService.GetStatisticsAsync(
            userId,
            weekStart,
            today,
            quantizationDays: 1,
            cancellationToken).ConfigureAwait(false);
        if (weeklyCaloriesResult.IsFailure) {
            return Result.Failure<GamificationModel>(weeklyCaloriesResult.Error);
        }

        IGamificationUserProfile userProfile = userProfileResult.Value;
        double weeklyAdherence = GamificationCalculator.CalculateWeeklyAdherence(
            ToDailyCalories(weeklyCaloriesResult.Value), userProfile.GetCalorieTargetForDate, today);

        IReadOnlyList<BadgeModel> badges = GamificationCalculator.CalculateBadges(longestStreak, totalMeals);
        int healthScore = GamificationCalculator.CalculateHealthScore(currentStreak, weeklyAdherence, totalMeals);

        return Result.Success(new GamificationModel(
            currentStreak,
            longestStreak,
            totalMeals,
            healthScore,
            weeklyAdherence,
            badges));
    }

    private static IReadOnlyDictionary<DateTime, double> ToDailyCalories(IReadOnlyList<DashboardStatisticsBucketReadModel> buckets) =>
        buckets
            .Where(static bucket => bucket.TotalCalories > 0)
            .ToDictionary(static bucket => bucket.DateFrom.Date, static bucket => bucket.TotalCalories);
}
