using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Admin.Services;

internal sealed class AdminUserManagementService(
    IUserReadRepository userReadRepository,
    IUserWriteRepository userWriteRepository,
    IUserRoleCatalogService roleCatalogService) : IAdminUserManagementService {
    public Task<User?> GetByIdIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken = default) =>
        userReadRepository.GetByIdIncludingDeletedAsync(userId, cancellationToken);

    public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) =>
        roleCatalogService.GetRolesByNamesAsync(names, cancellationToken);

    public Task UpdateAsync(
        User user,
        IReadOnlyCollection<UserRoleAuditEvent> roleAuditEvents,
        CancellationToken cancellationToken = default) =>
        userWriteRepository.UpdateAsync(user, roleAuditEvents, cancellationToken);
}
