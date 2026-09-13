using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Modules.Admin.Application.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminDashboardSummary;

public sealed class GetAdminDashboardSummaryQueryHandler(IAdminDashboardReadService readService)
    : IQueryHandler<GetAdminDashboardSummaryQuery, Result<AdminDashboardSummaryModel>> {
    public async Task<Result<AdminDashboardSummaryModel>> Handle(
        GetAdminDashboardSummaryQuery query,
        CancellationToken cancellationToken) {
        return await readService.GetSummaryAsync(query.RecentLimit, cancellationToken).ConfigureAwait(false);
    }
}
