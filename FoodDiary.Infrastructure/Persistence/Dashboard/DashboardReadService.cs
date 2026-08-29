using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Persistence.Dashboard;

internal sealed class DashboardReadService(
    IDashboardStatisticsReadService statisticsReadService,
    IDashboardBodyReadService bodyReadService,
    IDashboardMealsReadService mealsReadService) : IDashboardReadService {
    public async Task<Result<DashboardReadModel>> GetSnapshotDataAsync(
        UserId userId,
        DateTime dayStart,
        DateTime dayEnd,
        DateTime trendStart,
        int periodDays,
        int page,
        int pageSize,
        DashboardReadSections sections,
        CancellationToken cancellationToken = default) {
        DateTime weeklyFrom = dayStart;
        if (periodDays == 1 && !TemporalRangePolicy.TryAddDays(dayStart, -6, out weeklyFrom)) {
            return Result.Failure<DashboardReadModel>(
                Errors.Validation.Invalid(nameof(dayStart), "Dashboard weekly range is outside the supported date range."));
        }

        var statistics =
            Result.Success<IReadOnlyList<DashboardStatisticsBucketReadModel>>([]);
        var weeklyStatistics =
            Result.Success<IReadOnlyList<DashboardStatisticsBucketReadModel>>([]);
        if (sections.IncludeStatistics) {
            if (periodDays == 1) {
                weeklyStatistics = await statisticsReadService
                    .GetStatisticsAsync(userId, weeklyFrom, dayEnd, 1, cancellationToken)
                    .ConfigureAwait(false);
                if (weeklyStatistics.IsSuccess) {
                    statistics = Result.Success<IReadOnlyList<DashboardStatisticsBucketReadModel>>([
                        .. weeklyStatistics.Value.Where(bucket => bucket.DateFrom >= dayStart && bucket.DateFrom <= dayEnd),
                    ]);
                }
            } else {
                statistics = await statisticsReadService
                    .GetStatisticsAsync(userId, dayStart, dayEnd, periodDays, cancellationToken)
                    .ConfigureAwait(false);
                if (statistics.IsFailure) {
                    return Result.Failure<DashboardReadModel>(statistics.Error);
                }

                weeklyStatistics = await statisticsReadService
                    .GetStatisticsAsync(userId, weeklyFrom, dayEnd, 1, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        if (weeklyStatistics.IsFailure) {
            return Result.Failure<DashboardReadModel>(weeklyStatistics.Error);
        }

        Result<DashboardMealsReadModel> meals = sections.IncludeMeals
            ? await mealsReadService.GetMealsAsync(userId, page, pageSize, dayStart, dayEnd, cancellationToken).ConfigureAwait(false)
            : Result.Success(new DashboardMealsReadModel([], page, pageSize, 0, 0));
        if (meals.IsFailure) {
            return Result.Failure<DashboardReadModel>(meals.Error);
        }

        DashboardBodyReadModel body = await bodyReadService.GetBodyAsync(
            userId,
            dayStart,
            dayEnd,
            trendStart,
            trendQuantizationDays: 1,
            sections.IncludeWeight,
            sections.IncludeWaist,
            sections.IncludeHydration,
            cancellationToken).ConfigureAwait(false);

        return Result.Success(new DashboardReadModel(statistics.Value, weeklyStatistics.Value, body, meals.Value));
    }
}
