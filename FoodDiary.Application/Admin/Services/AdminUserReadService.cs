using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Admin.Services;

internal sealed class AdminUserReadService(
    IUserAdministrationReadService userAdministrationReadService) : IAdminUserReadService {
    public async Task<AdminUserModel?> GetByIdIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken = default) {
        UserAdminReadModel? user = await userAdministrationReadService
            .GetByIdIncludingDeletedAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        return user?.ToAdminModel();
    }

    public async Task<(IReadOnlyList<AdminUserModel> Items, int TotalItems)> GetPagedAsync(
        string? search,
        int page,
        int limit,
        UserAccountStatusFilter status,
        CancellationToken cancellationToken = default) {
        (IReadOnlyList<UserAdminReadModel> items, int totalItems) = await userAdministrationReadService.GetPagedAsync(
            search,
            page,
            limit,
            status,
            cancellationToken).ConfigureAwait(false);

        return ([.. items.Select(AdminUserMappings.ToAdminModel)], totalItems);
    }

    public async Task<AdminDashboardSummaryModel> GetDashboardSummaryAsync(
        int recentLimit,
        int pendingReportsCount,
        CancellationToken cancellationToken = default) {
        (int totalUsers, int activeUsers, int premiumUsers, int deletedUsers, IReadOnlyList<UserAdminReadModel> recentUsers) =
            await userAdministrationReadService.GetDashboardSummaryAsync(recentLimit, cancellationToken).ConfigureAwait(false);

        return new AdminDashboardSummaryModel(
            totalUsers,
            activeUsers,
            premiumUsers,
            deletedUsers,
            pendingReportsCount,
            [.. recentUsers.Select(AdminUserMappings.ToAdminModel)]);
    }

    public async Task<bool> ExistsIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken = default) =>
        await userAdministrationReadService.GetByIdIncludingDeletedAsync(userId, cancellationToken).ConfigureAwait(false) is not null;
}
