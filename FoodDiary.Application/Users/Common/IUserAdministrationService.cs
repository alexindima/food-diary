using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Common;

public interface IUserAdministrationService {
    Task<User?> GetByIdIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default);
    Task UpdateAsync(
        User user,
        IReadOnlyCollection<UserRoleAuditEvent> roleAuditEvents,
        CancellationToken cancellationToken = default);
}
