using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Common;

public interface IUserAdministrationReadService {
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
