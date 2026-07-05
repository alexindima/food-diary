using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Authentication.Services;

internal sealed class AuthenticationUserLookupService(IUserLookupRepository userRepository) : IAuthenticationUserLookupService {
    public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) =>
        userRepository.GetByEmailIncludingDeletedAsync(email, cancellationToken);

    public Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default) =>
        userRepository.GetByIdAsync(userId, cancellationToken);

    public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) =>
        userRepository.GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
}
