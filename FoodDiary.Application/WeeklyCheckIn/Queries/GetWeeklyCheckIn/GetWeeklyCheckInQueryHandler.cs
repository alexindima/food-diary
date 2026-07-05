using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.WeeklyCheckIn.Common;
using FoodDiary.Application.WeeklyCheckIn.Models;
using FoodDiary.Application.WeeklyCheckIn.Services;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.WeeklyCheckIn.Queries.GetWeeklyCheckIn;

public sealed class GetWeeklyCheckInQueryHandler(
    IMealReadRepository mealRepository,
    IDashboardStatisticsReadService statisticsReadService,
    IWeightEntryReadRepository weightEntryRepository,
    IWaistEntryReadRepository waistEntryRepository,
    IHydrationEntryReadRepository hydrationEntryRepository,
    IWeeklyCheckInUserProfileService weeklyCheckInUserProfileService,
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
        Result<WeeklyCheckInUserProfile> profileResult = await weeklyCheckInUserProfileService.GetAsync(userId, cancellationToken).ConfigureAwait(false);
        if (profileResult.IsFailure) {
            return Result.Failure<WeeklyCheckInModel>(profileResult.Error);
        }

        WeeklyCheckInUserProfile profile = profileResult.Value;
        DateTime today = dateTimeProvider.GetUtcNow().UtcDateTime.Date;
        DateTime thisWeekStart = today.AddDays(-6);
        DateTime lastWeekStart = thisWeekStart.AddDays(-7);
        DateTime lastWeekEnd = thisWeekStart.AddDays(-1);

        Result<WeekSummaryModel> thisWeekSummaryResult = await LoadWeekSummaryAsync(userId, thisWeekStart, today, cancellationToken).ConfigureAwait(false);
        if (thisWeekSummaryResult.IsFailure) {
            return Result.Failure<WeeklyCheckInModel>(thisWeekSummaryResult.Error);
        }

        Result<WeekSummaryModel> lastWeekSummaryResult = await LoadWeekSummaryAsync(userId, lastWeekStart, lastWeekEnd, cancellationToken).ConfigureAwait(false);
        if (lastWeekSummaryResult.IsFailure) {
            return Result.Failure<WeeklyCheckInModel>(lastWeekSummaryResult.Error);
        }

        WeekSummaryModel thisWeekSummary = thisWeekSummaryResult.Value;
        WeekSummaryModel lastWeekSummary = lastWeekSummaryResult.Value;
        WeekTrendModel trends = WeeklyCheckInCalculator.BuildTrends(thisWeekSummary, lastWeekSummary);
        IReadOnlyList<string> suggestions = WeeklyCheckInCalculator.GenerateSuggestions(thisWeekSummary, trends, profile.DailyCalorieTarget);

        return Result.Success(new WeeklyCheckInModel(thisWeekSummary, lastWeekSummary, trends, suggestions));
    }

    private async Task<Result<WeekSummaryModel>> LoadWeekSummaryAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken) {
        Result<IReadOnlyList<DashboardStatisticsBucketReadModel>> nutritionResult = await statisticsReadService.GetStatisticsAsync(
            userId,
            dateFrom,
            dateTo,
            quantizationDays: 1,
            cancellationToken).ConfigureAwait(false);
        if (nutritionResult.IsFailure) {
            return Result.Failure<WeekSummaryModel>(nutritionResult.Error);
        }

        int mealCount = await mealRepository.GetCountAsync(
            userId,
            new MealQueryFilters(DateFrom: dateFrom, DateTo: dateTo),
            cancellationToken).ConfigureAwait(false);
        IReadOnlyList<WeightEntry> weights = await weightEntryRepository.GetByPeriodAsync(userId, dateFrom, dateTo, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<WaistEntry> waists = await waistEntryRepository.GetByPeriodAsync(userId, dateFrom, dateTo, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<(DateTime Date, int TotalMl)> hydration = await hydrationEntryRepository.GetDailyTotalsAsync(userId, dateFrom, dateTo, cancellationToken).ConfigureAwait(false);

        return Result.Success(WeeklyCheckInCalculator.BuildSummary(nutritionResult.Value, mealCount, weights, waists, hydration, daysInPeriod: 7));
    }
}
