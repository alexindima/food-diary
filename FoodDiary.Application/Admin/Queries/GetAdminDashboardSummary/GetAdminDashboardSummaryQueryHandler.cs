using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Admin.Queries.GetAdminDashboardSummary;

public sealed class GetAdminDashboardSummaryQueryHandler(
    IUserRepository userRepository,
    IContentReportRepository contentReportRepository)
    : IQueryHandler<GetAdminDashboardSummaryQuery, Result<AdminDashboardSummaryModel>> {
    public async Task<Result<AdminDashboardSummaryModel>> Handle(
        GetAdminDashboardSummaryQuery query,
        CancellationToken cancellationToken) {
        var (totalUsers, activeUsers, premiumUsers, deletedUsers, recentUsers) =
            await userRepository.GetAdminDashboardSummaryAsync(query.RecentLimit, cancellationToken);

        var pendingReportsCount = await contentReportRepository.CountByStatusAsync(
            ReportStatus.Pending, cancellationToken);

        var response = new AdminDashboardSummaryModel(
            totalUsers,
            activeUsers,
            premiumUsers,
            deletedUsers,
            pendingReportsCount,
            recentUsers.Select(AdminUserMappings.ToAdminModel).ToArray());

        return Result.Success(response);
    }
}
