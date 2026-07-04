using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;

public interface IUserAdminReadRepository {
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

    Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)>
        GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default);
}
