using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminDashboardSummary;

public sealed class GetAdminDashboardSummaryQueryHandler(IAdminDashboardReadService readService)
    : IQueryHandler<GetAdminDashboardSummaryQuery, Result<AdminDashboardSummaryModel>> {
    public async Task<Result<AdminDashboardSummaryModel>> Handle(
        GetAdminDashboardSummaryQuery query,
        CancellationToken cancellationToken) {
        return await readService.GetSummaryAsync(query.RecentLimit, cancellationToken).ConfigureAwait(false);
    }
}
