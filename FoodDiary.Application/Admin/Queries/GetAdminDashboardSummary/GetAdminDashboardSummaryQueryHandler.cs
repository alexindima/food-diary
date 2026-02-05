using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Contracts.Admin;

namespace FoodDiary.Application.Admin.Queries.GetAdminDashboardSummary;

public sealed class GetAdminDashboardSummaryQueryHandler(IUserRepository userRepository)
    : IQueryHandler<GetAdminDashboardSummaryQuery, Result<AdminDashboardSummaryResponse>>
{
    public async Task<Result<AdminDashboardSummaryResponse>> Handle(
        GetAdminDashboardSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var (totalUsers, activeUsers, premiumUsers, deletedUsers, recentUsers) =
            await userRepository.GetAdminDashboardSummaryAsync(query.RecentLimit);

        var response = new AdminDashboardSummaryResponse(
            totalUsers,
            activeUsers,
            premiumUsers,
            deletedUsers,
            recentUsers.Select(AdminUserMappings.ToAdminResponse).ToArray());

        return Result.Success(response);
    }
}
