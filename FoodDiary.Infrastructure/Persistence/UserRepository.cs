using Microsoft.EntityFrameworkCore;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Infrastructure.Persistence;

public class UserRepository : IUserRepository
{
    private readonly FoodDiaryDbContext _context;

    public UserRepository(FoodDiaryDbContext context) => _context = context;

    private IQueryable<User> UsersWithRoles() =>
        _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role);

    public async Task<User?> GetByEmailAsync(string email) =>
        await UsersWithRoles().FirstOrDefaultAsync(u =>
            u.Email == email && u.IsActive && u.DeletedAt == null);

    public async Task<User?> GetByEmailIncludingDeletedAsync(string email) =>
        await UsersWithRoles().FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> GetByIdAsync(UserId id) =>
        await UsersWithRoles().FirstOrDefaultAsync(u =>
            u.Id == id && u.IsActive && u.DeletedAt == null);

    public async Task<User?> GetByIdIncludingDeletedAsync(UserId id) =>
        await UsersWithRoles().FirstOrDefaultAsync(u => u.Id == id);

    public async Task<User?> GetByTelegramUserIdAsync(long telegramUserId) =>
        await UsersWithRoles().FirstOrDefaultAsync(u =>
            u.TelegramUserId == telegramUserId && u.IsActive && u.DeletedAt == null);

    public async Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId) =>
        await UsersWithRoles().FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId);

    public async Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(
        string? search,
        int page,
        int limit,
        bool includeDeleted)
    {
        var query = UsersWithRoles().AsQueryable();

        if (!includeDeleted)
        {
            query = query.Where(u => u.DeletedAt == null);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(term) ||
                (u.Username ?? string.Empty).ToLower().Contains(term) ||
                (u.FirstName ?? string.Empty).ToLower().Contains(term) ||
                (u.LastName ?? string.Empty).ToLower().Contains(term));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(u => u.CreatedOnUtc)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        return (items, total);
    }

    public async Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)>
        GetAdminDashboardSummaryAsync(int recentLimit)
    {
        var totalUsers = await _context.Users.CountAsync();
        var activeUsers = await _context.Users.CountAsync(u => u.IsActive && u.DeletedAt == null);
        var deletedUsers = await _context.Users.CountAsync(u => u.DeletedAt != null);
        var premiumUsers = await _context.UserRoles
            .Where(ur => ur.Role.Name == RoleNames.Premium)
            .Select(ur => ur.UserId)
            .Distinct()
            .CountAsync();

        var recentUsers = await UsersWithRoles()
            .Where(u => u.DeletedAt == null)
            .OrderByDescending(u => u.CreatedOnUtc)
            .Take(recentLimit)
            .ToListAsync();

        return (totalUsers, activeUsers, premiumUsers, deletedUsers, recentUsers);
    }

    public async Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names)
    {
        return await _context.Roles
            .Where(role => names.Contains(role.Name))
            .ToListAsync();
    }

    public async Task<User> AddAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
}
