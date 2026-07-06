using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserAdminReadRepository {
    Task<UserAdminReadModel?> GetByIdIncludingDeletedReadModelAsync(
        UserId id,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(
        string? search,
        int page,
        int limit,
        bool includeDeleted,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(
        string? search,
        int page,
        int limit,
        UserAccountStatusFilter status,
        CancellationToken cancellationToken = default) =>
        GetPagedAsync(search, page, limit, status is UserAccountStatusFilter.All or UserAccountStatusFilter.Deleted, cancellationToken);

    Task<(IReadOnlyList<UserAdminReadModel> Items, int TotalItems)> GetPagedReadModelsAsync(
        string? search,
        int page,
        int limit,
        UserAccountStatusFilter status,
        CancellationToken cancellationToken = default);

    Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)>
        GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default);

    Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<UserAdminReadModel> RecentUsers)>
        GetAdminDashboardSummaryReadModelsAsync(int recentLimit, CancellationToken cancellationToken = default);
}
