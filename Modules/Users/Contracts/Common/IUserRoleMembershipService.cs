using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Contracts.Common;

public interface IUserRoleMembershipService {
    Task EnsureRoleAsync(UserId userId, string roleName, CancellationToken cancellationToken = default);

    Task RemoveRoleAsync(UserId userId, string roleName, CancellationToken cancellationToken = default);
}
