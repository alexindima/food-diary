using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Services;

public sealed class UserAdministrationReadService(IUserAdminReadModelRepository repository)
    : IUserAdministrationReadService {
    public Task<UserAdminReadModel?> GetByIdIncludingDeletedAsync(
        UserId userId,
        CancellationToken cancellationToken) =>
        repository.GetByIdIncludingDeletedReadModelAsync(userId, cancellationToken);

    public Task<(IReadOnlyList<UserAdminReadModel> Items, int TotalItems)> GetPagedAsync(
        string? search,
        int page,
        int limit,
        UserAccountStatusFilter status,
        CancellationToken cancellationToken) =>
        repository.GetPagedReadModelsAsync(search, page, limit, status, cancellationToken);

    public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<UserAdminReadModel> RecentUsers)>
        GetDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken) =>
        repository.GetAdminDashboardSummaryReadModelsAsync(recentLimit, cancellationToken);
}
