using FoodDiary.Domain.Entities;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Common.Interfaces.Persistence;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByEmailIncludingDeletedAsync(string email);
    Task<User?> GetByIdAsync(UserId id);
    Task<User?> GetByTelegramUserIdAsync(long telegramUserId);
    Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId);
    Task<User> AddAsync(User user);
    Task UpdateAsync(User user);
}
