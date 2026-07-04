using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Gamification.Common;
using FoodDiary.Application.Gamification.Models;
using FoodDiary.Application.Gamification.Services;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Meals;

namespace FoodDiary.Application.Gamification.Queries.GetGamification;

public class GetGamificationQueryHandler(
    IMealReadRepository mealRepository,
    IGamificationUserProfileService userProfileService,
    TimeProvider dateTimeProvider)
    : IQueryHandler<GetGamificationQuery, Result<GamificationModel>> {
    public async Task<Result<GamificationModel>> Handle(
        GetGamificationQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<GamificationModel>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
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
        IReadOnlyList<Meal> weekMeals = await mealRepository.GetByPeriodAsync(userId, weekStart, today, cancellationToken).ConfigureAwait(false);
        IGamificationUserProfile userProfile = userProfileResult.Value;
        double weeklyAdherence = GamificationCalculator.CalculateWeeklyAdherence(
            weekMeals, userProfile.GetCalorieTargetForDate, today);

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
}
