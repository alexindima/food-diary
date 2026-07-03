using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Authentication.Common;

public interface IAuthenticationUserLookupService {
    Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default);
}
