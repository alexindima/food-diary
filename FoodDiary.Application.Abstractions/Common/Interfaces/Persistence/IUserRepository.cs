using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;

public interface IUserRepository {
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default);
    Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default);
    Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default);
    Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default);

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

    Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default);
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    async Task EnsureRoleAsync(User user, string roleName, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

        string normalizedRoleName = roleName.Trim();
        if (user.HasRole(normalizedRoleName)) {
            return;
        }

        IReadOnlyList<string> roleNames = [
            .. user.GetRoleNames().Append(normalizedRoleName).Distinct(StringComparer.Ordinal),
        ];
        IReadOnlyList<Role> roles = await GetRolesByNamesAsync(roleNames, cancellationToken).ConfigureAwait(false);
        user.ReplaceRoles(roles);
        await UpdateAsync(user, cancellationToken).ConfigureAwait(false);
    }

    async Task RemoveRoleAsync(User user, string roleName, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

        string normalizedRoleName = roleName.Trim();
        if (!user.HasRole(normalizedRoleName)) {
            return;
        }

        IReadOnlyList<string> roleNames = [
            .. user.GetRoleNames().Where(name => !string.Equals(name, normalizedRoleName, StringComparison.Ordinal)),
        ];
        IReadOnlyList<Role> roles = await GetRolesByNamesAsync(roleNames, cancellationToken).ConfigureAwait(false);
        user.ReplaceRoles(roles);
        await UpdateAsync(user, cancellationToken).ConfigureAwait(false);
    }

    Task UpdateAsync(
        User user,
        IReadOnlyCollection<UserRoleAuditEvent> roleAuditEvents,
        CancellationToken cancellationToken = default) =>
        UpdateAsync(user, cancellationToken);
}
