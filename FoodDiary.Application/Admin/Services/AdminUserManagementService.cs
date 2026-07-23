using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Admin.Services;

internal sealed class AdminUserManagementService(IUserAdministrationService userAdministrationService)
    : IAdminUserManagementService {
    public Task<User?> GetByIdIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken = default) =>
        userAdministrationService.GetByIdIncludingDeletedAsync(userId, cancellationToken);

    public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) =>
        userAdministrationService.GetByEmailIncludingDeletedAsync(email, cancellationToken);

    public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) =>
        userAdministrationService.AddAsync(user, cancellationToken);

    public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) =>
        userAdministrationService.GetRolesByNamesAsync(names, cancellationToken);

    public Task UpdateAsync(
        User user,
        IReadOnlyCollection<UserRoleAuditEvent> roleAuditEvents,
        CancellationToken cancellationToken = default) =>
        userAdministrationService.UpdateAsync(user, roleAuditEvents, cancellationToken);
}
