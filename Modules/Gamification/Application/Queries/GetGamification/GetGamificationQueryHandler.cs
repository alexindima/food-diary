using FoodDiary.Modules.Gamification.Application.Services;
using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Results;
using FoodDiary.Modules.Gamification.Application.Abstractions.Achievements.Common;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Modules.Gamification.Application.Common;
using FoodDiary.Modules.Gamification.Application.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Users.Common;

namespace FoodDiary.Modules.Gamification.Application.Queries.GetGamification;

public sealed class GetGamificationQueryHandler(
    IMealActivityReadService mealActivityReadService,
    IMealNutritionStatisticsReadService statisticsReadService,
    IUserGamificationProfileReadService userProfileReadService,
    IAchievementMetricReader achievementMetricReader,
    IAchievementAwardService achievementAwardService,
    TimeProvider dateTimeProvider,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetGamificationQuery, Result<GamificationModel>> {
    public async Task<Result<GamificationModel>> Handle(
        GetGamificationQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<GamificationModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Result<UserGamificationProfileModel> userProfileResult =
            await userProfileReadService.GetGamificationProfileAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userProfileResult.IsFailure) {
            return Result.Failure<GamificationModel>(userProfileResult.Error);
        }

        DateTime today = dateTimeProvider.GetUtcNow().UtcDateTime.Date;
        DateTime streakFrom = DateTime.UnixEpoch;

        IReadOnlyList<DateTime> mealDates = await mealActivityReadService.GetDistinctMealDatesAsync(userId, streakFrom, today, cancellationToken).ConfigureAwait(false);
        (int currentStreak, int longestStreak) = GamificationCalculator.CalculateStreaks(mealDates, today);

        int totalMeals = await mealActivityReadService.GetTotalMealCountAsync(userId, cancellationToken).ConfigureAwait(false);
        int totalAcademyArticlesRead = await achievementMetricReader
            .GetCompletedAcademyArticleCountAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        DateTime weekStart = today.AddDays(-6);
        Result<IReadOnlyList<MealNutritionStatisticsBucket>> weeklyCaloriesResult = await statisticsReadService.GetStatisticsAsync(
            userId,
            weekStart,
            today,
            quantizationDays: 1,
            cancellationToken).ConfigureAwait(false);
        if (weeklyCaloriesResult.IsFailure) {
            return Result.Failure<GamificationModel>(weeklyCaloriesResult.Error);
        }

        UserGamificationProfileModel userProfile = userProfileResult.Value;
        double weeklyAdherence = GamificationCalculator.CalculateWeeklyAdherence(
            ToDailyCalories(weeklyCaloriesResult.Value), userProfile.CalorieSchedule.GetTargetForDate, today);

        IReadOnlyList<BadgeModel> badges = await achievementAwardService
            .EvaluateAndGrantAsync(
                userId,
                new AchievementMetricSnapshot(longestStreak, totalMeals, totalAcademyArticlesRead),
                cancellationToken)
            .ConfigureAwait(false);
        int healthScore = GamificationCalculator.CalculateHealthScore(currentStreak, weeklyAdherence, totalMeals);

        return Result.Success(new GamificationModel(
            currentStreak,
            longestStreak,
            totalMeals,
            healthScore,
            weeklyAdherence,
            badges));
    }

    private static IReadOnlyDictionary<DateTime, double> ToDailyCalories(IReadOnlyList<MealNutritionStatisticsBucket> buckets) =>
        buckets
            .Where(static bucket => bucket.TotalCalories > 0)
            .ToDictionary(static bucket => bucket.DateFrom.Date, static bucket => bucket.TotalCalories);
}
