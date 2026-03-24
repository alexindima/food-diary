using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;

namespace FoodDiary.Application.Admin.Queries.GetAdminDashboardSummary;

public sealed class GetAdminDashboardSummaryQueryHandler(IUserRepository userRepository)
    : IQueryHandler<GetAdminDashboardSummaryQuery, Result<AdminDashboardSummaryModel>> {
    public async Task<Result<AdminDashboardSummaryModel>> Handle(
        GetAdminDashboardSummaryQuery query,
        CancellationToken cancellationToken) {
        var (totalUsers, activeUsers, premiumUsers, deletedUsers, recentUsers) =
            await userRepository.GetAdminDashboardSummaryAsync(query.RecentLimit);

        var response = new AdminDashboardSummaryModel(
            totalUsers,
            activeUsers,
            premiumUsers,
            deletedUsers,
            recentUsers.Select(AdminUserMappings.ToAdminModel).ToArray());

        return Result.Success(response);
    }
}
