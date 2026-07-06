using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserAdminReadModelRepository {
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