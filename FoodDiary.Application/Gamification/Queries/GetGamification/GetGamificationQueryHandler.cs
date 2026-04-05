using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Gamification.Models;
using FoodDiary.Application.Gamification.Services;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Application.Users.Common;

namespace FoodDiary.Application.Gamification.Queries.GetGamification;

public class GetGamificationQueryHandler(
    IMealRepository mealRepository,
    IUserRepository userRepository,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetGamificationQuery, Result<GamificationModel>> {
    public async Task<Result<GamificationModel>> Handle(
        GetGamificationQuery query,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<GamificationModel>(userIdResult.Error);
        }

        var userId = userIdResult.Value;
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<GamificationModel>(accessError);
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        var today = dateTimeProvider.UtcNow.Date;
        var streakFrom = today.AddDays(-365);

        var mealDates = await mealRepository.GetDistinctMealDatesAsync(userId, streakFrom, today, cancellationToken);
        var (currentStreak, longestStreak) = GamificationCalculator.CalculateStreaks(mealDates, today);

        var totalMeals = await mealRepository.GetTotalMealCountAsync(userId, cancellationToken);

        var weekStart = today.AddDays(-6);
        var weekMeals = await mealRepository.GetByPeriodAsync(userId, weekStart, today, cancellationToken);
        var weeklyAdherence = GamificationCalculator.CalculateWeeklyAdherence(
            weekMeals, date => user?.GetCalorieTargetForDate(date), today);

        var badges = GamificationCalculator.CalculateBadges(longestStreak, totalMeals);
        var healthScore = GamificationCalculator.CalculateHealthScore(currentStreak, weeklyAdherence, totalMeals);

        return Result.Success(new GamificationModel(
            currentStreak,
            longestStreak,
            totalMeals,
            healthScore,
            weeklyAdherence,
            badges));
    }
}
