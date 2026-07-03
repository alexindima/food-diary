using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Admin.Services;

internal sealed class AdminUserManagementService(IUserRepository userRepository) : IAdminUserManagementService {
    public Task<User?> GetByIdIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken = default) =>
        userRepository.GetByIdIncludingDeletedAsync(userId, cancellationToken);

    public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) =>
        userRepository.GetRolesByNamesAsync(names, cancellationToken);

    public Task UpdateAsync(
        User user,
        IReadOnlyCollection<UserRoleAuditEvent> roleAuditEvents,
        CancellationToken cancellationToken = default) =>
        userRepository.UpdateAsync(user, roleAuditEvents, cancellationToken);
}
