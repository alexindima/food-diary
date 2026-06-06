using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.WeeklyCheckIn.Models;
using FoodDiary.Application.WeeklyCheckIn.Services;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.WeeklyCheckIn.Queries.GetWeeklyCheckIn;

public class GetWeeklyCheckInQueryHandler(
    IMealRepository mealRepository,
    IWeightEntryRepository weightEntryRepository,
    IWaistEntryRepository waistEntryRepository,
    IHydrationEntryRepository hydrationEntryRepository,
    IUserRepository userRepository,
    TimeProvider dateTimeProvider)
    : IQueryHandler<GetWeeklyCheckInQuery, Result<WeeklyCheckInModel>> {
    public async Task<Result<WeeklyCheckInModel>> Handle(
        GetWeeklyCheckInQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<WeeklyCheckInModel>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<WeeklyCheckInModel>(accessError);
        }

        User? user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        DateTime today = dateTimeProvider.GetUtcNow().UtcDateTime.Date;
        DateTime thisWeekStart = today.AddDays(-6);
        DateTime lastWeekStart = thisWeekStart.AddDays(-7);
        DateTime lastWeekEnd = thisWeekStart.AddDays(-1);

        IReadOnlyList<Meal> thisWeekMeals = await mealRepository.GetByPeriodAsync(userId, thisWeekStart, today, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<Meal> lastWeekMeals = await mealRepository.GetByPeriodAsync(userId, lastWeekStart, lastWeekEnd, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<WeightEntry> thisWeekWeights = await weightEntryRepository.GetByPeriodAsync(userId, thisWeekStart, today, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<WeightEntry> lastWeekWeights = await weightEntryRepository.GetByPeriodAsync(userId, lastWeekStart, lastWeekEnd, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<WaistEntry> thisWeekWaists = await waistEntryRepository.GetByPeriodAsync(userId, thisWeekStart, today, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<WaistEntry> lastWeekWaists = await waistEntryRepository.GetByPeriodAsync(userId, lastWeekStart, lastWeekEnd, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<(DateTime Date, int TotalMl)> thisWeekHydration = await hydrationEntryRepository.GetDailyTotalsAsync(userId, thisWeekStart, today, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<(DateTime Date, int TotalMl)> lastWeekHydration = await hydrationEntryRepository.GetDailyTotalsAsync(userId, lastWeekStart, lastWeekEnd, cancellationToken).ConfigureAwait(false);

        WeekSummaryModel thisWeekSummary = WeeklyCheckInCalculator.BuildSummary(thisWeekMeals, thisWeekWeights, thisWeekWaists, thisWeekHydration, 7);
        WeekSummaryModel lastWeekSummary = WeeklyCheckInCalculator.BuildSummary(lastWeekMeals, lastWeekWeights, lastWeekWaists, lastWeekHydration, 7);

        WeekTrendModel trends = WeeklyCheckInCalculator.BuildTrends(thisWeekSummary, lastWeekSummary);
        IReadOnlyList<string> suggestions = WeeklyCheckInCalculator.GenerateSuggestions(thisWeekSummary, trends, user?.DailyCalorieTarget);

        return Result.Success(new WeeklyCheckInModel(thisWeekSummary, lastWeekSummary, trends, suggestions));
    }
}
