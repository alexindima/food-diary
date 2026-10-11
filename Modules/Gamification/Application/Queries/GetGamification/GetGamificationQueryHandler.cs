using FoodDiary.Application.Contracts.Common.Validation;
using FoodDiary.Application.Contracts.Common.Abstractions.Results;
using FoodDiary.Mediator;
using FoodDiary.Modules.Meals.Contracts.Queries.ReadDistinctMealDates;
using FoodDiary.Modules.Meals.Contracts.Queries.ReadTotalMealCount;
using FoodDiary.Modules.Gamification.Application.Services;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Results;
using FoodDiary.Modules.Gamification.Application.Abstractions.Achievements.Common;
using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Modules.Gamification.Application.Common;
using FoodDiary.Modules.Gamification.Application.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Contracts.Common;

namespace FoodDiary.Modules.Gamification.Application.Queries.GetGamification;

public sealed class GetGamificationQueryHandler(
    ISender mealActivityReadService,
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

        if (!LocalCalendar.TryResolve(query.TimeZoneId, offsetMinutes: null, out TimeZoneInfo timeZone)) {
            return Result.Failure<GamificationModel>(Errors.Validation.Invalid(nameof(query.TimeZoneId), "Provide a valid calendar time zone."));
        }
        var today = LocalCalendar.DateAt(dateTimeProvider.GetUtcNow().UtcDateTime, timeZone).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        DateTime dayEnd = LocalCalendar.StartOfDayUtc(DateOnly.FromDateTime(today).AddDays(1), timeZone).AddTicks(-1);
        DateTime streakFrom = DateTime.UnixEpoch;

        IReadOnlyList<DateTime> mealDates = await mealActivityReadService.Send(new ReadDistinctMealDatesQuery(userId, streakFrom, query.TimeZoneId is null ? today : dayEnd, query.TimeZoneId is null ? null : timeZone), cancellationToken).ConfigureAwait(false);
        (int currentStreak, int longestStreak) = GamificationCalculator.CalculateStreaks(mealDates, today);

        int totalMeals = await mealActivityReadService.Send(new ReadTotalMealCountQuery(userId), cancellationToken).ConfigureAwait(false);
        int totalAcademyArticlesRead = await achievementMetricReader
            .GetCompletedAcademyArticleCountAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        DateTime weekStart = today.AddDays(-6);
        Result<IReadOnlyList<MealNutritionStatisticsBucket>> weeklyCaloriesResult = await statisticsReadService.GetStatisticsAsync(
            userId,
            LocalCalendar.StartOfDayUtc(DateOnly.FromDateTime(weekStart), timeZone),
            query.TimeZoneId is null ? today : dayEnd,
            quantizationDays: 1,
            cancellationToken, query.TimeZoneId is null ? null : timeZone).ConfigureAwait(false);
        if (weeklyCaloriesResult.IsFailure) {
            return Result.Failure<GamificationModel>(weeklyCaloriesResult.Error);
        }

        UserGamificationProfileModel userProfile = userProfileResult.Value;
        double weeklyAdherence = GamificationCalculator.CalculateWeeklyAdherence(
            ToDailyCalories(weeklyCaloriesResult.Value, timeZone), userProfile.CalorieSchedule.GetTargetForDate, today);

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

    private static IReadOnlyDictionary<DateTime, double> ToDailyCalories(IReadOnlyList<MealNutritionStatisticsBucket> buckets, TimeZoneInfo timeZone) =>
        buckets
            .Where(static bucket => bucket.TotalCalories > 0)
            .ToDictionary(bucket => LocalCalendar.DateAt(bucket.DateFrom, timeZone).ToDateTime(TimeOnly.MinValue), static bucket => bucket.TotalCalories);
}
