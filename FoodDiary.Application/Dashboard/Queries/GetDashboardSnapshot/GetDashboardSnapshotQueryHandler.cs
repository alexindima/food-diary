using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Dashboard.Common;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Dashboard.Services;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dashboard.Queries.GetDashboardSnapshot;

public sealed class GetDashboardSnapshotQueryHandler(
    IDashboardSnapshotBuilder snapshotBuilder,
    IDashboardUserContextService dashboardUserContextService)
    : IQueryHandler<GetDashboardSnapshotQuery, Result<DashboardSnapshotModel>> {
    public async Task<Result<DashboardSnapshotModel>> Handle(GetDashboardSnapshotQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            dashboardUserContextService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<DashboardSnapshotModel>(userIdResult);
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
