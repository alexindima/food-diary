using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminDashboardOverview;

public sealed class GetAdminDashboardOverviewQueryHandler(IAdminDashboardOverviewReadService service)
    : IQueryHandler<GetAdminDashboardOverviewQuery, Result<AdminDashboardOverviewModel>> {
    public Task<Result<AdminDashboardOverviewModel>> Handle(GetAdminDashboardOverviewQuery query, CancellationToken cancellationToken) =>
        service.GetAsync(query.From, query.To, query.AllTime, cancellationToken);
}
