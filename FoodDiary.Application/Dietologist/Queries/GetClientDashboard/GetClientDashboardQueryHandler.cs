using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Queries.GetClientDashboard;

public sealed class GetClientDashboardQueryHandler(IDietologistClientReadService readService)
    : IQueryHandler<GetClientDashboardQuery, Result<DashboardSnapshotModel>> {
    public async Task<Result<DashboardSnapshotModel>> Handle(
        GetClientDashboardQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<DashboardSnapshotModel>(userIdResult);
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
