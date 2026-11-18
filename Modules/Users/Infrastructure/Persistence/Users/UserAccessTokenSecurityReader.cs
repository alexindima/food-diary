using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Users.Infrastructure.Persistence.Users;

public sealed class UserAccessTokenSecurityReader(DbSet<User> users, Func<CancellationToken, Task>? synchronizeTransactionAsync = null) : IUserAccessTokenSecurityReader {
    public Task<bool> IsCurrentAsync(Guid userId, long securityVersion, CancellationToken cancellationToken = default) =>
        IsCurrentAsync(userId, securityVersion, allowRequiredPasswordChange: false, cancellationToken);

    public Task<bool> IsCurrentForPasswordChangeAsync(Guid userId, long securityVersion, CancellationToken cancellationToken = default) =>
        IsCurrentAsync(userId, securityVersion, allowRequiredPasswordChange: true, cancellationToken);

    private async Task<bool> IsCurrentAsync(
        Guid userId,
        long securityVersion,
        bool allowRequiredPasswordChange,
        CancellationToken cancellationToken) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        return await users.AsNoTracking().AnyAsync(
            user => user.Id == new UserId(userId) &&
                    user.IsActive &&
                    user.DeletedAt == null &&
                    (allowRequiredPasswordChange || !user.MustChangePassword) &&
                    user.SecurityVersion == securityVersion,
            cancellationToken);
    }

    public async Task<bool> IsCurrentAdministratorAsync(
        Guid userId,
        long securityVersion,
        CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }

        return await users.AsNoTracking().AnyAsync(
            user => user.Id == new UserId(userId) &&
                    user.IsActive &&
                    user.DeletedAt == null &&
                    !user.MustChangePassword &&
                    user.SecurityVersion == securityVersion &&
                    user.UserRoles.Any(userRole => userRole.Role.Name == RoleNames.Admin),
            cancellationToken);
    }
}
