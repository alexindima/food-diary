using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Queries.GetClientDashboard;

public sealed class GetClientDashboardQueryHandler(IDietologistClientReadService readService)
    : IQueryHandler<GetClientDashboardQuery, Result<DashboardSnapshotModel>> {
    public async Task<Result<DashboardSnapshotModel>> Handle(
        GetClientDashboardQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<DashboardSnapshotModel>(Errors.Authentication.InvalidToken);
        }

        var dietologistUserId = new UserId(query.UserId!.Value);
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
