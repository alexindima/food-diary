using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Authentication.Services;

internal sealed class AuthenticationUserMutationService(IUserRepository userRepository) : IAuthenticationUserMutationService {
    public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) =>
        userRepository.AddAsync(user, cancellationToken);

    public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) =>
        userRepository.GetByEmailIncludingDeletedAsync(email, cancellationToken);

    public Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default) =>
        userRepository.GetByIdAsync(userId, cancellationToken);

    public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) =>
        userRepository.GetByTelegramUserIdIncludingDeletedAsync(telegramUserId, cancellationToken);

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default) =>
        userRepository.UpdateAsync(user, cancellationToken);
}
