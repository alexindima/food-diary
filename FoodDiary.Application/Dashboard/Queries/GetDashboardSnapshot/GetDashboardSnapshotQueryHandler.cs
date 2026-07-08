using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Dashboard.Services;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dashboard.Queries.GetDashboardSnapshot;

public sealed class GetDashboardSnapshotQueryHandler(IDashboardSnapshotBuilder snapshotBuilder)
    : IQueryHandler<GetDashboardSnapshotQuery, Result<DashboardSnapshotModel>> {
    public async Task<Result<DashboardSnapshotModel>> Handle(GetDashboardSnapshotQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<DashboardSnapshotModel>(userIdResult);
        }

        return await snapshotBuilder.BuildAsync(
            new DashboardSnapshotRequest(
                userIdResult.Value.Value,
                query.Date,
                DateTo: null,
                query.Locale,
                query.TrendDays,
                query.Page,
                query.PageSize),
            cancellationToken).ConfigureAwait(false);
    }
}
