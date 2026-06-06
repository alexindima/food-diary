using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Gamification.Models;
using FoodDiary.Application.Gamification.Services;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Entities.Meals;

namespace FoodDiary.Application.Gamification.Queries.GetGamification;

public class GetGamificationQueryHandler(
    IMealRepository mealRepository,
    IUserRepository userRepository,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetGamificationQuery, Result<GamificationModel>> {
    public async Task<Result<GamificationModel>> Handle(
        GetGamificationQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<GamificationModel>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<GamificationModel>(accessError);
        }

        User? user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);

        DateTime today = dateTimeProvider.UtcNow.Date;
        DateTime streakFrom = today.AddDays(-365);

        IReadOnlyList<DateTime> mealDates = await mealRepository.GetDistinctMealDatesAsync(userId, streakFrom, today, cancellationToken).ConfigureAwait(false);
        (int currentStreak, int longestStreak) = GamificationCalculator.CalculateStreaks(mealDates, today);

        int totalMeals = await mealRepository.GetTotalMealCountAsync(userId, cancellationToken).ConfigureAwait(false);

        DateTime weekStart = today.AddDays(-6);
        IReadOnlyList<Meal> weekMeals = await mealRepository.GetByPeriodAsync(userId, weekStart, today, cancellationToken).ConfigureAwait(false);
        double weeklyAdherence = GamificationCalculator.CalculateWeeklyAdherence(
            weekMeals, date => user?.GetCalorieTargetForDate(date), today);

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
