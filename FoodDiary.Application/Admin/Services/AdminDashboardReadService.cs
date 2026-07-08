using FoodDiary.Results;
using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Admin.Services;

public sealed class AdminDashboardReadService(
    IAdminUserReadService userReadService,
    IContentReportReadRepository contentReportRepository)
    : IAdminDashboardReadService {
    public async Task<Result<AdminDashboardSummaryModel>> GetSummaryAsync(
        int recentLimit,
        CancellationToken cancellationToken) {
        int pendingReportsCount = await contentReportRepository.CountByStatusAsync(
            ReportStatus.Pending, cancellationToken).ConfigureAwait(false);

        AdminDashboardSummaryModel response = await userReadService.GetDashboardSummaryAsync(
            recentLimit,
            pendingReportsCount,
            cancellationToken).ConfigureAwait(false);

        return Result.Success(response);
    }
}
