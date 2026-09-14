using FoodDiary.Mediator;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistEntries;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightEntries;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Hydration.Common;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;
using FoodDiary.Application.WeeklyCheckIn.Common;
using FoodDiary.Application.WeeklyCheckIn.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeeklyCheckIn.Services;

public sealed class WeeklyCheckInReadService(
    IMealActivityReadService mealActivityReadService, IDashboardStatisticsReadService statisticsReadService, ISender sender, IHydrationEntryReadService hydrationEntryReadService)
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

        IReadOnlyList<WeightEntryModel> weights = await sender.Send(new ReadWeightEntriesQuery(UserId: userId, DateFrom: dateFrom, DateTo: dateTo, Limit: null, Descending: false), cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<WaistEntryModel> waists = await sender.Send(new ReadWaistEntriesQuery(UserId: userId, DateFrom: dateFrom, DateTo: dateTo, Limit: null, Descending: false), cancellationToken)
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
