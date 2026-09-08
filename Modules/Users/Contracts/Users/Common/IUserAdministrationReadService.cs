using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserAdministrationReadService {
    Task<(IReadOnlyList<UserAdminReadModel> Items, int TotalItems)> GetFilteredPagedAsync(
        string? search, int page, int limit, UserAccountStatusFilter status,
        UserAdministrationFilter filter, CancellationToken cancellationToken);

    Task<UserAdminReadModel?> GetByIdIncludingDeletedAsync(
        UserId userId,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<UserAdminReadModel> Items, int TotalItems)> GetPagedAsync(
        string? search,
        int page,
        int limit,
        UserAccountStatusFilter status,
        CancellationToken cancellationToken);

    Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<UserAdminReadModel> RecentUsers)>
        GetDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken);
}
