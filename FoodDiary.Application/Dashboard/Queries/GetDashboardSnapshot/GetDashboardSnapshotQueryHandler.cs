using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Dashboard.Services;

namespace FoodDiary.Application.Dashboard.Queries.GetDashboardSnapshot;

public class GetDashboardSnapshotQueryHandler(IDashboardSnapshotBuilder snapshotBuilder)
    : IQueryHandler<GetDashboardSnapshotQuery, Result<DashboardSnapshotModel>> {
    public async Task<Result<DashboardSnapshotModel>> Handle(GetDashboardSnapshotQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<DashboardSnapshotModel>(Errors.Authentication.InvalidToken);
        }

        return await snapshotBuilder.BuildAsync(
            new DashboardSnapshotRequest(
                query.UserId.Value,
                query.Date,
                query.Locale,
                query.TrendDays,
                query.Page,
                query.PageSize),
            cancellationToken);
    }
}
