using Microsoft.EntityFrameworkCore;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Persistence.Users;

public class UserRepository(FoodDiaryDbContext context) : IUserRepository {
    private const string LikeEscapeCharacter = "\\";

    private IQueryable<User> UsersWithRoles() =>
        context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await UsersWithRoles().FirstOrDefaultAsync(u =>
            u.Email == email && u.IsActive && u.DeletedAt == null, cancellationToken);

    public async Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) =>
        await UsersWithRoles().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) =>
        await UsersWithRoles().FirstOrDefaultAsync(u =>
            u.Id == id && u.IsActive && u.DeletedAt == null, cancellationToken);

    public async Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) =>
        await UsersWithRoles().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) =>
        await UsersWithRoles().FirstOrDefaultAsync(u =>
            u.TelegramUserId == telegramUserId && u.IsActive && u.DeletedAt == null, cancellationToken);

    public async Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) =>
        await UsersWithRoles().FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId, cancellationToken);

    public async Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(
        string? search,
        int page,
        int limit,
        bool includeDeleted,
        CancellationToken cancellationToken = default) {
        var pageNumber = Math.Max(page, 1);
        var pageSize = Math.Max(limit, 1);
        var filteredQuery = context.Users.AsQueryable();

        if (!includeDeleted) {
            filteredQuery = filteredQuery.Where(u => u.DeletedAt == null);
        }

        if (!string.IsNullOrWhiteSpace(search)) {
            var term = $"%{EscapeLikePattern(search.Trim())}%";
            filteredQuery = filteredQuery.Where(u =>
                EF.Functions.ILike(u.Email, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(u.Username ?? string.Empty, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(u.FirstName ?? string.Empty, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(u.LastName ?? string.Empty, term, LikeEscapeCharacter));
        }

        var total = await filteredQuery.CountAsync(cancellationToken);
        var pageIds = await filteredQuery
            .OrderByDescending(u => u.CreatedOnUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (pageIds.Count == 0) {
            return ([], total);
        }

        var usersById = await UsersWithRoles()
            .Where(u => pageIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var items = pageIds
            .Select(id => usersById[id])
            .ToList();

        return (items, total);
    }

    public async Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)>
        GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) {
        var userCounts = await context.Users
            .GroupBy(_ => 1)
            .Select(group => new {
                TotalUsers = group.Count(),
                ActiveUsers = group.Count(u => u.IsActive && u.DeletedAt == null),
                DeletedUsers = group.Count(u => u.DeletedAt != null)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var premiumUsers = await context.UserRoles
            .Where(ur => ur.Role.Name == RoleNames.Premium)
            .Select(ur => ur.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        var recentUsers = await UsersWithRoles()
            .Where(u => u.DeletedAt == null)
            .OrderByDescending(u => u.CreatedOnUtc)
            .Take(recentLimit)
            .ToListAsync(cancellationToken);

        return (
            userCounts?.TotalUsers ?? 0,
            userCounts?.ActiveUsers ?? 0,
            premiumUsers,
            userCounts?.DeletedUsers ?? 0,
            recentUsers);
    }

    public async Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) {
        return await context.Roles
            .Where(role => names.Contains(role.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default) {
        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default) {
        context.Users.Update(user);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static string EscapeLikePattern(string value) {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
