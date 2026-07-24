using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Audit.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Queries.GetClientDashboard;

public sealed class GetClientDashboardQueryHandler(
    IDietologistClientReadService readService,
    IAuditEntryWriter auditWriter,
    IUnitOfWork unitOfWork,
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
        Result<DashboardSnapshotModel> result = await readService.GetDashboardAsync(
            dietologistUserId,
            query.ClientUserId,
            query.Date,
            query.DateTo,
            query.Locale,
            query.TrendDays,
            query.Page,
            query.PageSize,
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure) {
            return result;
        }

        await auditWriter.AddAsync(
            dietologistUserId,
            query.ClientUserId,
            "dietologist.dashboard.accessed",
            "ClientDashboard",
            query.ClientUserId.ToString(),
            metadata: null,
            cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }
}
