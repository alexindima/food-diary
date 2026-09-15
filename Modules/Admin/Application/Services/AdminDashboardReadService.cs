using FoodDiary.Mediator;
using FoodDiary.Application.ContentReports.Queries.CountContentReports;
using FoodDiary.Application.Abstractions.Users.Queries.GetUserAdministrationSummary;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Modules.Admin.Application.Mappings;
using FoodDiary.Results;
using FoodDiary.Modules.Admin.Application.Common;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Modules.Admin.Application.Services;

public sealed class AdminDashboardReadService(
    ISender sender)
    : IAdminDashboardReadService {
    public async Task<Result<AdminDashboardSummaryModel>> GetSummaryAsync(
        int recentLimit,
        CancellationToken cancellationToken) {
        int pendingReportsCount = await sender.Send(new CountContentReportsQuery(Status: ReportStatus.Pending), cancellationToken).ConfigureAwait(false);

        (int totalUsers, int activeUsers, int premiumUsers, int deletedUsers, IReadOnlyList<UserAdminReadModel> recentUsers) =
            await sender.Send(new GetUserAdministrationSummaryQuery(RecentLimit: recentLimit), cancellationToken).ConfigureAwait(false);
        var response = new AdminDashboardSummaryModel(totalUsers, activeUsers, premiumUsers, deletedUsers,
            pendingReportsCount, [.. recentUsers.Select(AdminUserMappings.ToAdminModel)]);

        return Result.Success(response);
    }
}
