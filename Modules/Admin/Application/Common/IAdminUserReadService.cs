using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Admin.Common;

public interface IAdminUserReadService {
    Task<AdminUserModel?> GetByIdIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<AdminUserModel> Items, int TotalItems)> GetPagedAsync(
        string? search,
        int page,
        int limit,
        UserAccountStatusFilter status,
        CancellationToken cancellationToken = default);

    Task<AdminDashboardSummaryModel> GetDashboardSummaryAsync(
        int recentLimit,
        int pendingReportsCount,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken = default);
}
