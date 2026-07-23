using Microsoft.EntityFrameworkCore;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Persistence.Users;

public sealed class UserRepository(FoodDiaryDbContext context) : IUserRepository, IGoogleIdentityUserDirectoryService, IUserAdminReadRepository, IUserAdminReadModelRepository {
    private const string LikeEscapeCharacter = "\\";

    private IQueryable<User> UsersWithRoles() =>
        context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await UsersWithRoles().FirstOrDefaultAsync(u =>
            u.Email == email && u.IsActive && u.DeletedAt == null, cancellationToken).ConfigureAwait(false);

    public async Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) =>
        await UsersWithRoles().FirstOrDefaultAsync(u => u.Email == email, cancellationToken).ConfigureAwait(false);

    public async Task<User?> GetByGoogleIdentityIncludingDeletedAsync(string issuer, string subject, CancellationToken cancellationToken = default) =>
        await UsersWithRoles().FirstOrDefaultAsync(u =>
            u.GoogleIssuer == issuer && u.GoogleSubject == subject, cancellationToken).ConfigureAwait(false);

    public async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) =>
        await UsersWithRoles().FirstOrDefaultAsync(u =>
            u.Id == id && u.IsActive && u.DeletedAt == null, cancellationToken).ConfigureAwait(false);

    public async Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) =>
        await UsersWithRoles().FirstOrDefaultAsync(u => u.Id == id, cancellationToken).ConfigureAwait(false);

    public async Task<UserAdminReadModel?> GetByIdIncludingDeletedReadModelAsync(
        UserId id,
        CancellationToken cancellationToken = default) {
        User? user = await GetByIdIncludingDeletedAsync(id, cancellationToken).ConfigureAwait(false);
        return user is null ? null : ToAdminReadModel(user);
    }

    public async Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) =>
        await UsersWithRoles().FirstOrDefaultAsync(u =>
            u.TelegramUserId == telegramUserId && u.IsActive && u.DeletedAt == null, cancellationToken).ConfigureAwait(false);

    public async Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) =>
        await UsersWithRoles().FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId, cancellationToken).ConfigureAwait(false);

    public async Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(
        string? search,
        int page,
        int limit,
        bool includeDeleted,
        CancellationToken cancellationToken = default) {
        UserAccountStatusFilter status = includeDeleted ? UserAccountStatusFilter.All : UserAccountStatusFilter.Active;
        return await GetPagedAsync(search, page, limit, status, cancellationToken).ConfigureAwait(false);
    }

    public async Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(
        string? search,
        int page,
        int limit,
        UserAccountStatusFilter status,
        CancellationToken cancellationToken = default) {
        int pageNumber = Math.Max(page, 1);
        int pageSize = Math.Max(limit, 1);
        IQueryable<User> filteredQuery = context.Users.AsQueryable();

        filteredQuery = status switch {
            UserAccountStatusFilter.Active => filteredQuery.Where(u => u.IsActive && u.DeletedAt == null),
            UserAccountStatusFilter.Inactive => filteredQuery.Where(u => !u.IsActive && u.DeletedAt == null),
            UserAccountStatusFilter.Deleted => filteredQuery.Where(u => u.DeletedAt != null),
            _ => filteredQuery,
        };

        if (!string.IsNullOrWhiteSpace(search)) {
            string term = $"%{EscapeLikePattern(search.Trim())}%";
            filteredQuery = filteredQuery.Where(u =>
                EF.Functions.ILike(u.Email, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(u.Username ?? string.Empty, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(u.FirstName ?? string.Empty, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(u.LastName ?? string.Empty, term, LikeEscapeCharacter));
        }

        int total = await filteredQuery.CountAsync(cancellationToken).ConfigureAwait(false);
        List<UserId> pageIds = await filteredQuery
            .OrderByDescending(u => u.CreatedOnUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (pageIds.Count == 0) {
            return ([], total);
        }

        Dictionary<UserId, User> usersById = await UsersWithRoles()
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
        (IReadOnlyList<User> Items, int TotalItems) = await GetPagedAsync(
            search,
            page,
            limit,
            status,
            cancellationToken).ConfigureAwait(false);

        return ([.. Items.Select(ToAdminReadModel)], TotalItems);
    }

    public async Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)>
        GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) {
        var userCounts = await context.Users
            .GroupBy(_ => 1)
            .Select(group => new {
                TotalUsers = group.Count(),
                ActiveUsers = group.Count(u => u.IsActive && u.DeletedAt == null),
                DeletedUsers = group.Count(u => u.DeletedAt != null),
            })
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        int premiumUsers = await context.UserRoles
            .Where(ur => ur.Role.Name == RoleNames.Premium)
            .Select(ur => ur.UserId)
            .Distinct()
            .CountAsync(cancellationToken).ConfigureAwait(false);

        List<User> recentUsers = await UsersWithRoles()
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
        (int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers) =
            await GetAdminDashboardSummaryAsync(recentLimit, cancellationToken).ConfigureAwait(false);

        return (TotalUsers, ActiveUsers, PremiumUsers, DeletedUsers, [.. RecentUsers.Select(ToAdminReadModel)]);
    }

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default) {
        await context.Users.AddAsync(user, cancellationToken).ConfigureAwait(false);
        return user;
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default) {
        TrackForUpdate(user);
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(
        User user,
        IReadOnlyCollection<UserRoleAuditEvent> roleAuditEvents,
        CancellationToken cancellationToken = default) {
        TrackForUpdate(user);
        if (roleAuditEvents.Count > 0) {
            await context.UserRoleAuditEvents.AddRangeAsync(roleAuditEvents, cancellationToken).ConfigureAwait(false);
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private void TrackForUpdate(User user) {
        if (context.Entry(user).State == EntityState.Detached) {
            context.Users.Update(user);
        }
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
            user.Weight,
            user.DesiredWeight,
            user.DesiredWaist,
            user.Height,
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
