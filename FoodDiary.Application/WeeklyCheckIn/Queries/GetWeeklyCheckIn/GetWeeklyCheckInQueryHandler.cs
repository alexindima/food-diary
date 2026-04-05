using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Hydration.Common;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.WaistEntries.Common;
using FoodDiary.Application.WeeklyCheckIn.Models;
using FoodDiary.Application.WeeklyCheckIn.Services;
using FoodDiary.Application.WeightEntries.Common;

namespace FoodDiary.Application.WeeklyCheckIn.Queries.GetWeeklyCheckIn;

public class GetWeeklyCheckInQueryHandler(
    IMealRepository mealRepository,
    IWeightEntryRepository weightEntryRepository,
    IWaistEntryRepository waistEntryRepository,
    IHydrationEntryRepository hydrationEntryRepository,
    IUserRepository userRepository,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetWeeklyCheckInQuery, Result<WeeklyCheckInModel>> {
    public async Task<Result<WeeklyCheckInModel>> Handle(
        GetWeeklyCheckInQuery query,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<WeeklyCheckInModel>(userIdResult.Error);
        }

        var userId = userIdResult.Value;
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<WeeklyCheckInModel>(accessError);
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        var today = dateTimeProvider.UtcNow.Date;
        var thisWeekStart = today.AddDays(-6);
        var lastWeekStart = thisWeekStart.AddDays(-7);
        var lastWeekEnd = thisWeekStart.AddDays(-1);

        var thisWeekMeals = await mealRepository.GetByPeriodAsync(userId, thisWeekStart, today, cancellationToken);
        var lastWeekMeals = await mealRepository.GetByPeriodAsync(userId, lastWeekStart, lastWeekEnd, cancellationToken);

        var thisWeekWeights = await weightEntryRepository.GetByPeriodAsync(userId, thisWeekStart, today, cancellationToken);
        var lastWeekWeights = await weightEntryRepository.GetByPeriodAsync(userId, lastWeekStart, lastWeekEnd, cancellationToken);

        var thisWeekWaists = await waistEntryRepository.GetByPeriodAsync(userId, thisWeekStart, today, cancellationToken);
        var lastWeekWaists = await waistEntryRepository.GetByPeriodAsync(userId, lastWeekStart, lastWeekEnd, cancellationToken);

        var thisWeekHydration = await hydrationEntryRepository.GetDailyTotalsAsync(userId, thisWeekStart, today, cancellationToken);
        var lastWeekHydration = await hydrationEntryRepository.GetDailyTotalsAsync(userId, lastWeekStart, lastWeekEnd, cancellationToken);

        var thisWeekSummary = WeeklyCheckInCalculator.BuildSummary(thisWeekMeals, thisWeekWeights, thisWeekWaists, thisWeekHydration, 7);
        var lastWeekSummary = WeeklyCheckInCalculator.BuildSummary(lastWeekMeals, lastWeekWeights, lastWeekWaists, lastWeekHydration, 7);

        var trends = WeeklyCheckInCalculator.BuildTrends(thisWeekSummary, lastWeekSummary);
        var suggestions = WeeklyCheckInCalculator.GenerateSuggestions(thisWeekSummary, trends, user?.DailyCalorieTarget);

        return Result.Success(new WeeklyCheckInModel(thisWeekSummary, lastWeekSummary, trends, suggestions));
    }
}
