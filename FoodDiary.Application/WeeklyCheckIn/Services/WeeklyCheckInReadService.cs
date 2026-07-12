using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Hydration.Common;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Application.WaistEntries.Common;
using FoodDiary.Application.WeeklyCheckIn.Common;
using FoodDiary.Application.WeeklyCheckIn.Models;
using FoodDiary.Application.WeightEntries.Common;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeeklyCheckIn.Services;

public sealed class WeeklyCheckInReadService(
    IMealActivityReadService mealActivityReadService,
    IDashboardStatisticsReadService statisticsReadService,
    IWeightEntryReadService weightEntryReadService,
    IWaistEntryReadService waistEntryReadService,
    IHydrationEntryReadService hydrationEntryReadService)
    : IWeeklyCheckInReadService {
    public async Task<Result<WeekSummaryModel>> LoadWeekSummaryAsync(
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

        int mealCount = await mealActivityReadService.GetCountAsync(
            userId,
            new MealQueryFilters(DateFrom: dateFrom, DateTo: dateTo),
            cancellationToken).ConfigureAwait(false);

        IReadOnlyList<WeightEntryModel> weights = await weightEntryReadService
            .GetEntriesAsync(userId, dateFrom, dateTo, limit: null, descending: false, cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<WaistEntryModel> waists = await waistEntryReadService
            .GetEntriesAsync(userId, dateFrom, dateTo, limit: null, descending: false, cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<(DateTime Date, int TotalMl)> hydration = await hydrationEntryReadService
            .GetDailyTotalsAsync(userId, dateFrom, dateTo, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(WeeklyCheckInCalculator.BuildSummary(
            nutritionResult.Value,
            mealCount,
            weights,
            waists,
            hydration,
            daysInPeriod: 7));
    }
}
