using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Authentication.Services;

internal sealed class AuthenticationUserRegistrationService(
    IUserReadRepository userReadRepository,
    IUserWriteRepository userWriteRepository,
    IUserRoleCatalogService roleCatalogService) : IAuthenticationUserRegistrationService {
    public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) =>
        userWriteRepository.AddAsync(user, cancellationToken);

    public Task<IReadOnlyList<Role>> EnsureRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) =>
        roleCatalogService.EnsureRolesByNamesAsync(names, cancellationToken);

    public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) =>
        userReadRepository.GetByEmailIncludingDeletedAsync(email, cancellationToken);
}
