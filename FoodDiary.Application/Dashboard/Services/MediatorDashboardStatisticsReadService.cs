using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Statistics.Models;
using FoodDiary.Application.Statistics.Queries.GetStatistics;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Dashboard.Services;

internal sealed class MediatorDashboardStatisticsReadService(ISender sender) : IDashboardStatisticsReadService {
    public async Task<Result<IReadOnlyList<DashboardStatisticsBucketReadModel>>> GetStatisticsAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        int quantizationDays,
        CancellationToken cancellationToken = default) {
        Result<IReadOnlyList<AggregatedStatisticsModel>> result = await sender.Send(
            new GetStatisticsQuery(userId.Value, dateFrom, dateTo, quantizationDays),
            cancellationToken).ConfigureAwait(false);

        if (result.IsFailure) {
            return Result.Failure<IReadOnlyList<DashboardStatisticsBucketReadModel>>(result.Error);
        }

        return Result.Success<IReadOnlyList<DashboardStatisticsBucketReadModel>>([.. result.Value.Select(ToReadModel)]);
    }

    private static DashboardStatisticsBucketReadModel ToReadModel(AggregatedStatisticsModel model) =>
        new(
            model.DateFrom,
            model.DateTo,
            model.TotalCalories,
            model.AverageProteins,
            model.AverageFats,
            model.AverageCarbs,
            model.AverageFiber,
            model.TotalProteins,
            model.TotalFats,
            model.TotalCarbs,
            model.TotalFiber);
}
