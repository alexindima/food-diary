using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dashboard.Application.Abstractions.Common;
using FoodDiary.Modules.Dashboard.Contracts.Models;
using FoodDiary.Modules.Dashboard.Contracts.Queries.ReadDashboardStatistics;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dashboard.Application.Queries.ReadDashboardStatistics;

public sealed class ReadDashboardStatisticsQueryHandler(IDashboardStatisticsReadService statistics)
    : IQueryHandler<ReadDashboardStatisticsQuery, Result<IReadOnlyList<DashboardStatisticsBucketReadModel>>> {
    public Task<Result<IReadOnlyList<DashboardStatisticsBucketReadModel>>> Handle(
        ReadDashboardStatisticsQuery query, CancellationToken cancellationToken) =>
        statistics.GetStatisticsAsync(query.UserId, query.DateFrom, query.DateTo, query.QuantizationDays, cancellationToken);
}
