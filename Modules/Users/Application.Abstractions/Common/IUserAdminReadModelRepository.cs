using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Application.Abstractions.Common;

public interface IUserAdminReadModelRepository {
    Task<(IReadOnlyList<UserAdminReadModel> Items, int TotalItems)> GetFilteredPagedReadModelsAsync(
        string? search, int page, int limit, UserAccountStatusFilter status,
        UserAdministrationFilter filter, CancellationToken cancellationToken);

    Task<UserAdminReadModel?> GetByIdIncludingDeletedReadModelAsync(
        UserId id,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<UserAdminReadModel> Items, int TotalItems)> GetPagedReadModelsAsync(
        string? search,
        int page,
        int limit,
        UserAccountStatusFilter status,
        CancellationToken cancellationToken = default);

    Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<UserAdminReadModel> RecentUsers)>
        GetAdminDashboardSummaryReadModelsAsync(int recentLimit, CancellationToken cancellationToken = default);
}
