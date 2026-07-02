using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dashboard.Services;

internal sealed class ComposedDashboardReadService(
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
        Result<IReadOnlyList<DashboardStatisticsBucketReadModel>> statistics = sections.IncludeStatistics
            ? await statisticsReadService.GetStatisticsAsync(userId, dayStart, dayEnd, periodDays, cancellationToken).ConfigureAwait(false)
            : Result.Success<IReadOnlyList<DashboardStatisticsBucketReadModel>>([]);
        if (statistics.IsFailure) {
            return Result.Failure<DashboardReadModel>(statistics.Error);
        }

        DateTime weeklyFrom = periodDays == 1 ? dayStart.AddDays(-6) : dayStart;
        Result<IReadOnlyList<DashboardStatisticsBucketReadModel>> weeklyStatistics = sections.IncludeStatistics
            ? await statisticsReadService.GetStatisticsAsync(userId, weeklyFrom, dayEnd, 1, cancellationToken).ConfigureAwait(false)
            : Result.Success<IReadOnlyList<DashboardStatisticsBucketReadModel>>([]);
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
            dayEnd.Date,
            trendStart,
            trendQuantizationDays: 1,
            sections.IncludeWeight,
            sections.IncludeWaist,
            sections.IncludeHydration,
            cancellationToken).ConfigureAwait(false);

        return Result.Success(new DashboardReadModel(statistics.Value, weeklyStatistics.Value, body, meals.Value));
    }
}
