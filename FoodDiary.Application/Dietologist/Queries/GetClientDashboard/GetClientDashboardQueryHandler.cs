using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Queries.GetClientDashboard;

public sealed class GetClientDashboardQueryHandler(
    IDietologistClientReadService readService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetClientDashboardQuery, Result<DashboardSnapshotModel>> {
    public async Task<Result<DashboardSnapshotModel>> Handle(
        GetClientDashboardQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<DashboardSnapshotModel>(userIdResult);
        }

        UserId dietologistUserId = userIdResult.Value;
        return await readService.GetDashboardAsync(
            dietologistUserId,
            query.ClientUserId,
            query.Date,
            query.DateTo,
            query.Locale,
            query.TrendDays,
            query.Page,
            query.PageSize,
            cancellationToken).ConfigureAwait(false);
    }
}
