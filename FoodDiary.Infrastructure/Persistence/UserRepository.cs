using Microsoft.EntityFrameworkCore;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Persistence;

public class UserRepository(FoodDiaryDbContext context) : IUserRepository {
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
        var query = UsersWithRoles().AsQueryable();

        if (!includeDeleted) {
            query = query.Where(u => u.DeletedAt == null);
        }

        if (!string.IsNullOrWhiteSpace(search)) {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(term) ||
                (u.Username ?? string.Empty).ToLower().Contains(term) ||
                (u.FirstName ?? string.Empty).ToLower().Contains(term) ||
                (u.LastName ?? string.Empty).ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(u => u.CreatedOnUtc)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)>
        GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) {
        var totalUsers = await context.Users.CountAsync(cancellationToken);
        var activeUsers = await context.Users.CountAsync(u => u.IsActive && u.DeletedAt == null, cancellationToken);
        var deletedUsers = await context.Users.CountAsync(u => u.DeletedAt != null, cancellationToken);
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

        return (totalUsers, activeUsers, premiumUsers, deletedUsers, recentUsers);
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
}
