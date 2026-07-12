using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Services;

internal sealed class UserAdministrationService(
    IUserDirectoryService userDirectoryService,
    IUserWriteRepository userWriteRepository,
    IUserRoleCatalogService roleCatalogService) : IUserAdministrationService {
    public Task<User?> GetByIdIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken = default) =>
        userDirectoryService.GetByIdIncludingDeletedAsync(userId, cancellationToken);

    public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(
        IReadOnlyList<string> names,
        CancellationToken cancellationToken = default) =>
        roleCatalogService.GetRolesByNamesAsync(names, cancellationToken);

    public Task UpdateAsync(
        User user,
        IReadOnlyCollection<UserRoleAuditEvent> roleAuditEvents,
        CancellationToken cancellationToken = default) =>
        userWriteRepository.UpdateAsync(user, roleAuditEvents, cancellationToken);
}
