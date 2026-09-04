using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserRoleMembershipService {
    Task EnsureRoleAsync(UserId userId, string roleName, CancellationToken cancellationToken = default);

    Task RemoveRoleAsync(UserId userId, string roleName, CancellationToken cancellationToken = default);
}
