using Microsoft.EntityFrameworkCore;
using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Infrastructure.Persistence.Users;

public sealed class UserAdministrationReadRepository(DbSet<User> users, DbSet<UserRole> userRoles, Func<CancellationToken, Task>? synchronizeTransactionAsync = null) : IUserAdminReadRepository, IUserAdminReadModelRepository {
    private const string LikeEscapeCharacter = "\\";

    private IQueryable<User> UsersWithRoles() =>
        users
            .AsSplitQuery()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role);

    public async Task<UserAdminReadModel?> GetByIdIncludingDeletedReadModelAsync(
        UserId id,
        CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        User? user = await UsersWithRoles()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            .ConfigureAwait(false);
        return user is null ? null : ToAdminReadModel(user);
    }

    public async Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(
        string? search,
        int page,
        int limit,
        bool includeDeleted,
        CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        UserAccountStatusFilter status = includeDeleted ? UserAccountStatusFilter.All : UserAccountStatusFilter.Active;
        return await GetPagedAsync(search, page, limit, status, cancellationToken).ConfigureAwait(false);
    }

    public async Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(
        string? search,
        int page,
        int limit,
        UserAccountStatusFilter status,
        CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        return await GetFilteredPageAsync(search, page, limit, status, new UserAdministrationFilter(), cancellationToken).ConfigureAwait(false);
    }

    public async Task<(IReadOnlyList<UserAdminReadModel> Items, int TotalItems)> GetFilteredPagedReadModelsAsync(
        string? search, int page, int limit, UserAccountStatusFilter status,
        UserAdministrationFilter filter, CancellationToken cancellationToken) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        (IReadOnlyList<User> items, int totalItems) = await GetFilteredPageAsync(search, page, limit, status, filter, cancellationToken).ConfigureAwait(false);
        return ([.. items.Select(ToAdminReadModel)], totalItems);
    }

    private async Task<(IReadOnlyList<User> Items, int TotalItems)> GetFilteredPageAsync(
        string? search, int page, int limit, UserAccountStatusFilter status,
        UserAdministrationFilter filter, CancellationToken cancellationToken) {
        int pageNumber = PaginationPolicy.NormalizePage(page);
        int pageSize = PaginationPolicy.NormalizePageSize(limit, defaultPageSize: 1);
        IQueryable<User> filteredQuery = users.AsNoTracking();
        if (filter.RegisteredFrom is { } registeredFrom) {
            var start = registeredFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            filteredQuery = filteredQuery.Where(user => user.CreatedOnUtc >= start);
        }
        if (filter.RegisteredTo is { } registeredTo) {
            var end = registeredTo.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            filteredQuery = filteredQuery.Where(user => user.CreatedOnUtc <= end);
        }
        if (filter.LastLoginFrom is { } loginFrom) {
            var start = loginFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            filteredQuery = filteredQuery.Where(user => user.LastLoginAtUtc >= start);
        }
        if (filter.LastLoginTo is { } loginTo) {
            var end = loginTo.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            filteredQuery = filteredQuery.Where(user => user.LastLoginAtUtc <= end);
        }
        if (filter.EmailConfirmed is { } confirmed) {
            filteredQuery = filteredQuery.Where(user => user.IsEmailConfirmed == confirmed);
        }
        if (!string.IsNullOrWhiteSpace(filter.Role)) {
            string role = filter.Role.Trim();
            filteredQuery = filteredQuery.Where(user => user.UserRoles.Any(membership => membership.Role.Name == role));
        }

        filteredQuery = status switch {
            UserAccountStatusFilter.Active => filteredQuery.Where(u => u.IsActive && u.DeletedAt == null),
            UserAccountStatusFilter.Inactive => filteredQuery.Where(u => !u.IsActive && u.DeletedAt == null),
            UserAccountStatusFilter.Deleted => filteredQuery.Where(u => u.DeletedAt != null),
            _ => filteredQuery,
        };

        if (!string.IsNullOrWhiteSpace(search)) {
            string term = $"%{EscapeLikePattern(search.Trim())}%";
            filteredQuery = filteredQuery.Where(u =>
                (u.Email != null && EF.Functions.ILike(u.Email, term, LikeEscapeCharacter)) ||
                EF.Functions.ILike(u.Username ?? string.Empty, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(u.FirstName ?? string.Empty, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(u.LastName ?? string.Empty, term, LikeEscapeCharacter));
        }

        int total = await filteredQuery.CountAsync(cancellationToken).ConfigureAwait(false);
        List<UserId> pageIds = await filteredQuery
            .OrderByDescending(u => u.CreatedOnUtc)
            .ThenBy(u => u.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (pageIds.Count == 0) {
            return ([], total);
        }

        Dictionary<UserId, User> usersById = await UsersWithRoles()
            .AsNoTracking()
            .Where(u => pageIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken).ConfigureAwait(false);

        List<User> items = pageIds.ConvertAll(id => usersById[id]);

        return (items, total);
    }

    public async Task<(IReadOnlyList<UserAdminReadModel> Items, int TotalItems)> GetPagedReadModelsAsync(
        string? search,
        int page,
        int limit,
        UserAccountStatusFilter status,
        CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        (IReadOnlyList<User> items, int totalItems) = await GetPagedAsync(
            search,
            page,
            limit,
            status,
            cancellationToken).ConfigureAwait(false);

        return ([.. items.Select(ToAdminReadModel)], totalItems);
    }

    public async Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)>
        GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        var userCounts = await users
            .GroupBy(_ => 1)
            .Select(group => new {
                TotalUsers = group.Count(),
                ActiveUsers = group.Count(u => u.IsActive && u.DeletedAt == null),
                DeletedUsers = group.Count(u => u.DeletedAt != null),
            })
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        int premiumUsers = await userRoles
            .Where(ur => ur.Role.Name == RoleNames.Premium)
            .Select(ur => ur.UserId)
            .Distinct()
            .CountAsync(cancellationToken).ConfigureAwait(false);

        List<User> recentUsers = await UsersWithRoles()
            .AsNoTracking()
            .Where(u => u.DeletedAt == null)
            .OrderByDescending(u => u.CreatedOnUtc)
            .Take(recentLimit)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return (
            userCounts?.TotalUsers ?? 0,
            userCounts?.ActiveUsers ?? 0,
            premiumUsers,
            userCounts?.DeletedUsers ?? 0,
            recentUsers);
    }

    public async Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<UserAdminReadModel> RecentUsers)>
        GetAdminDashboardSummaryReadModelsAsync(int recentLimit, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        (int totalUsers, int activeUsers, int premiumUsers, int deletedUsers, IReadOnlyList<User> recentUsers) =
            await GetAdminDashboardSummaryAsync(recentLimit, cancellationToken).ConfigureAwait(false);

        return (totalUsers, activeUsers, premiumUsers, deletedUsers, [.. recentUsers.Select(ToAdminReadModel)]);
    }

    private static string EscapeLikePattern(string value) {
        return value
            .Replace("\\", @"\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }

    private static UserAdminReadModel ToAdminReadModel(User user) =>
        new(
            user.Id.Value,
            user.Email,
            user.HasPassword,
            user.Username,
            user.FirstName,
            user.LastName,
            user.BirthDate,
            user.Gender,
            user.WeightKg,
            user.DesiredWeightKg,
            user.DesiredWaistCm,
            user.HeightCm,
            user.ActivityLevel.ToString(),
            user.DailyCalorieTarget,
            user.ProteinTarget,
            user.FatTarget,
            user.CarbTarget,
            user.FiberTarget,
            user.StepGoal,
            user.WaterGoal,
            user.HydrationGoal,
            user.CalorieCyclingEnabled,
            user.MondayCalories,
            user.TuesdayCalories,
            user.WednesdayCalories,
            user.ThursdayCalories,
            user.FridayCalories,
            user.SaturdayCalories,
            user.SundayCalories,
            user.ProfileImage,
            user.ProfileImageAssetId?.Value,
            user.DashboardLayoutJson,
            user.Language,
            user.Theme,
            user.UiStyle,
            user.PushNotificationsEnabled,
            user.FastingPushNotificationsEnabled,
            user.SocialPushNotificationsEnabled,
            user.FastingCheckInReminderHours,
            user.FastingCheckInFollowUpReminderHours,
            user.TelegramUserId,
            user.IsActive,
            user.IsEmailConfirmed,
            user.CreatedOnUtc,
            user.DeletedAt,
            user.LastLoginAtUtc,
            [.. user.GetRoleNames()],
            user.AiInputTokenLimit,
            user.AiOutputTokenLimit,
            user.AiConsentAcceptedAt,
            user.MustChangePassword);
}
