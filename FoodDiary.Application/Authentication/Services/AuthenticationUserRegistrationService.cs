using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Authentication.Services;

internal sealed class AuthenticationUserRegistrationService(
    IUserDirectoryService userDirectoryService,
    IUserIdentityMutationService userIdentityMutationService) : IAuthenticationUserRegistrationService {
    public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) =>
        userIdentityMutationService.AddAsync(user, cancellationToken);

    public Task<IReadOnlyList<Role>> EnsureRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) =>
        userIdentityMutationService.EnsureRolesByNamesAsync(names, cancellationToken);

    public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) =>
        userDirectoryService.GetByEmailIncludingDeletedAsync(email, cancellationToken);
}
