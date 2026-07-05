using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Admin.Queries.GetAdminDashboardSummary;

public sealed class GetAdminDashboardSummaryQueryHandler(
    IAdminUserReadService userReadService,
    IContentReportReadRepository contentReportRepository)
    : IQueryHandler<GetAdminDashboardSummaryQuery, Result<AdminDashboardSummaryModel>> {
    public async Task<Result<AdminDashboardSummaryModel>> Handle(
        GetAdminDashboardSummaryQuery query,
        CancellationToken cancellationToken) {
        int pendingReportsCount = await contentReportRepository.CountByStatusAsync(
            ReportStatus.Pending, cancellationToken).ConfigureAwait(false);

        AdminDashboardSummaryModel response = await userReadService.GetDashboardSummaryAsync(
            query.RecentLimit,
            pendingReportsCount,
            cancellationToken).ConfigureAwait(false);

        return Result.Success(response);
    }
}
