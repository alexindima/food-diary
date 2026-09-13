using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Modules.Admin.Application.Mappings;
using FoodDiary.Results;
using FoodDiary.Modules.Admin.Application.Common;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Application.ContentReports.Common;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Modules.Admin.Application.Services;

public sealed class AdminDashboardReadService(
    IUserAdministrationReadService userReadService,
    IContentReportAdministrationReadService contentReportReadService)
    : IAdminDashboardReadService {
    public async Task<Result<AdminDashboardSummaryModel>> GetSummaryAsync(
        int recentLimit,
        CancellationToken cancellationToken) {
        int pendingReportsCount = await contentReportReadService.CountAsync(
            ReportStatus.Pending, cancellationToken).ConfigureAwait(false);

        (int totalUsers, int activeUsers, int premiumUsers, int deletedUsers, IReadOnlyList<UserAdminReadModel> recentUsers) =
            await userReadService.GetDashboardSummaryAsync(recentLimit, cancellationToken).ConfigureAwait(false);
        var response = new AdminDashboardSummaryModel(totalUsers, activeUsers, premiumUsers, deletedUsers,
            pendingReportsCount, [.. recentUsers.Select(AdminUserMappings.ToAdminModel)]);

        return Result.Success(response);
    }
}
