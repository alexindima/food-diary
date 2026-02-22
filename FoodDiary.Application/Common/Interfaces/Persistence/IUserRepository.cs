using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Common.Interfaces.Persistence;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByEmailIncludingDeletedAsync(string email);
    Task<User?> GetByIdAsync(UserId id);
    Task<User?> GetByIdIncludingDeletedAsync(UserId id);
    Task<User?> GetByTelegramUserIdAsync(long telegramUserId);
    Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId);
    Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted);
    Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)>
        GetAdminDashboardSummaryAsync(int recentLimit);
    Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names);
    Task<User> AddAsync(User user);
    Task UpdateAsync(User user);
}

