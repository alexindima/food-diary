using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Application.Common;
using FoodDiary.Modules.Users.Domain.Entities;

namespace FoodDiary.Modules.Users.Application.Services;

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
