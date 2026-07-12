using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Users.Common;

namespace FoodDiary.Application.Admin.Services;

internal sealed class AdminUserReadService(
    IUserDirectoryService userLookupRepository,
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
        (IReadOnlyList<UserAdminReadModel> Items, int TotalItems) = await userAdministrationReadService.GetPagedAsync(
            search,
            page,
            limit,
            status,
            cancellationToken).ConfigureAwait(false);

        return ([.. Items.Select(AdminUserMappings.ToAdminModel)], TotalItems);
    }

    public async Task<AdminDashboardSummaryModel> GetDashboardSummaryAsync(
        int recentLimit,
        int pendingReportsCount,
        CancellationToken cancellationToken = default) {
        (int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<UserAdminReadModel> RecentUsers) =
            await userAdministrationReadService.GetDashboardSummaryAsync(recentLimit, cancellationToken).ConfigureAwait(false);

        return new AdminDashboardSummaryModel(
            TotalUsers,
            ActiveUsers,
            PremiumUsers,
            DeletedUsers,
            pendingReportsCount,
            [.. RecentUsers.Select(AdminUserMappings.ToAdminModel)]);
    }

    public async Task<bool> ExistsIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken = default) =>
        await userLookupRepository.GetByIdIncludingDeletedAsync(userId, cancellationToken).ConfigureAwait(false) is not null;
}
