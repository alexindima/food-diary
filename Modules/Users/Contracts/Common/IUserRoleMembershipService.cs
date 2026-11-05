using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Contracts.Common;

public interface IUserRoleMembershipService {
    Task SetPremiumAccessEndAsync(UserId userId, DateTime? endsAtUtc, CancellationToken cancellationToken = default) =>
        endsAtUtc is null ? Task.CompletedTask : throw new NotSupportedException("Timed Premium roles require an expiry-aware membership service.");

    Task EnsureRoleAsync(UserId userId, string roleName, CancellationToken cancellationToken = default);

    Task RemoveRoleAsync(UserId userId, string roleName, CancellationToken cancellationToken = default);
}
