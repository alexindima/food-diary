using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Authentication.Services;

internal sealed class AuthenticationUserMutationService(
    IUserLookupRepository userLookupRepository,
    IUserWriteRepository userWriteRepository) : IAuthenticationUserMutationService {
    public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) =>
        userWriteRepository.AddAsync(user, cancellationToken);

    public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) =>
        userLookupRepository.GetByEmailIncludingDeletedAsync(email, cancellationToken);

    public Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default) =>
        userLookupRepository.GetByIdAsync(userId, cancellationToken);

    public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) =>
        userLookupRepository.GetByTelegramUserIdIncludingDeletedAsync(telegramUserId, cancellationToken);

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default) =>
        userWriteRepository.UpdateAsync(user, cancellationToken);
}
