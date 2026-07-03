using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Admin.Services;

internal sealed class AdminUserReadService(IUserRepository userRepository) : IAdminUserReadService {
    public Task<User?> GetByIdIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken = default) =>
        userRepository.GetByIdIncludingDeletedAsync(userId, cancellationToken);

    public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(
        string? search,
        int page,
        int limit,
        UserAccountStatusFilter status,
        CancellationToken cancellationToken = default) =>
        userRepository.GetPagedAsync(search, page, limit, status, cancellationToken);

    public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetDashboardSummaryAsync(
        int recentLimit,
        CancellationToken cancellationToken = default) =>
        userRepository.GetAdminDashboardSummaryAsync(recentLimit, cancellationToken);
}
