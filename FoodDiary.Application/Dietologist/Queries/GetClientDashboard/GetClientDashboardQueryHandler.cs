using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Dashboard.Queries.GetDashboardSnapshot;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using MediatR;

namespace FoodDiary.Application.Dietologist.Queries.GetClientDashboard;

public class GetClientDashboardQueryHandler(
    IDietologistInvitationRepository invitationRepository,
    ISender mediator)
    : IQueryHandler<GetClientDashboardQuery, Result<DashboardSnapshotModel>> {
    public async Task<Result<DashboardSnapshotModel>> Handle(
        GetClientDashboardQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<DashboardSnapshotModel>(Errors.Authentication.InvalidToken);
        }

        var dietologistUserId = new UserId(query.UserId!.Value);
        var clientUserId = new UserId(query.ClientUserId);

        var accessResult = await DietologistAccessPolicy.EnsureCanAccessClientAsync(
            invitationRepository, dietologistUserId, clientUserId, cancellationToken);

        if (accessResult.IsFailure) {
            return Result.Failure<DashboardSnapshotModel>(accessResult.Error);
        }

        var permissionError = DietologistAccessPolicy.EnsurePermission(accessResult.Value, "Statistics");
        if (permissionError is not null) {
            return Result.Failure<DashboardSnapshotModel>(permissionError);
        }

        var dashboardQuery = new GetDashboardSnapshotQuery(
            query.ClientUserId,
            query.Date,
            query.Page,
            query.PageSize,
            query.Locale,
            query.TrendDays);

        return await mediator.Send(dashboardQuery, cancellationToken);
    }
}
