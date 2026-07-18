using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Authentication.Services;

internal sealed class AuthenticationUserMutationService(
    IUserDirectoryService userDirectoryService,
    IUserIdentityMutationService userIdentityMutationService) : IAuthenticationUserMutationService {
    public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) =>
        userIdentityMutationService.AddAsync(user, cancellationToken);

    public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) =>
        userDirectoryService.GetByEmailIncludingDeletedAsync(email, cancellationToken);

    public Task<User?> GetByGoogleIdentityIncludingDeletedAsync(string issuer, string subject, CancellationToken cancellationToken = default) =>
        userDirectoryService.GetByGoogleIdentityIncludingDeletedAsync(issuer, subject, cancellationToken);

    public Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default) =>
        userDirectoryService.GetByIdAsync(userId, cancellationToken);

    public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) =>
        userDirectoryService.GetByTelegramUserIdIncludingDeletedAsync(telegramUserId, cancellationToken);

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default) =>
        userIdentityMutationService.UpdateAsync(user, cancellationToken);
}
