using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Application.Abstractions.Common;

public interface IUserLookupRepository {
    async Task<DateTime?> GetRegistrationTimeAsync(UserId id, CancellationToken cancellationToken = default) =>
        (await GetByIdAsync(id, cancellationToken))?.CreatedOnUtc;

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default);
    Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default);
    Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default);
    Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default);
}
