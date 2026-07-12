using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Users.Services;

internal sealed class UserIdentityMutationService(
    IUserWriteRepository userWriteRepository,
    IUserRoleCatalogService roleCatalogService) : IUserIdentityMutationService {
    public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) =>
        userWriteRepository.AddAsync(user, cancellationToken);

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default) =>
        userWriteRepository.UpdateAsync(user, cancellationToken);

    public Task<IReadOnlyList<Role>> EnsureRolesByNamesAsync(
        IReadOnlyList<string> names,
        CancellationToken cancellationToken = default) =>
        roleCatalogService.EnsureRolesByNamesAsync(names, cancellationToken);
}
