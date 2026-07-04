using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Users;

internal sealed class UserRoleMembershipService(FoodDiaryDbContext context) : IUserRoleMembershipService {
    public async Task EnsureRoleAsync(UserId userId, string roleName, CancellationToken cancellationToken = default) {
        EnsureValidInput(userId, roleName);

        if (!context.Database.IsRelational()) {
            await EnsureRoleWithTrackedEntitiesAsync(userId, roleName.Trim(), cancellationToken).ConfigureAwait(false);
            return;
        }

        FormattableString sql = $"""
            INSERT INTO "UserRoles" ("UserId", "RoleId")
            SELECT {userId.Value}, "Id"
            FROM "Roles"
            WHERE "Name" = {roleName.Trim()}
            ON CONFLICT DO NOTHING
            """;
        await context.Database.ExecuteSqlInterpolatedAsync(sql, cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveRoleAsync(UserId userId, string roleName, CancellationToken cancellationToken = default) {
        EnsureValidInput(userId, roleName);

        string normalizedRoleName = roleName.Trim();
        if (!context.Database.IsRelational()) {
            await RemoveRoleWithTrackedEntitiesAsync(userId, normalizedRoleName, cancellationToken).ConfigureAwait(false);
            return;
        }

        await context.UserRoles
            .Where(userRole => userRole.UserId == userId && userRole.Role.Name == normalizedRoleName)
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureRoleWithTrackedEntitiesAsync(UserId userId, string roleName, CancellationToken cancellationToken) {
        Role? role = await context.Roles
            .FirstOrDefaultAsync(candidate => candidate.Name == roleName, cancellationToken).ConfigureAwait(false);
        if (role is null) {
            return;
        }

        bool alreadyAssigned = await context.UserRoles
            .AnyAsync(userRole => userRole.UserId == userId && userRole.RoleId == role.Id, cancellationToken).ConfigureAwait(false);
        if (alreadyAssigned) {
            return;
        }

        await context.UserRoles.AddAsync(new UserRole(userId, role.Id), cancellationToken).ConfigureAwait(false);
    }

    private async Task RemoveRoleWithTrackedEntitiesAsync(UserId userId, string roleName, CancellationToken cancellationToken) {
        UserRole? userRole = await context.UserRoles
            .FirstOrDefaultAsync(
                candidate => candidate.UserId == userId && candidate.Role.Name == roleName,
                cancellationToken).ConfigureAwait(false);
        if (userRole is null) {
            return;
        }

        context.UserRoles.Remove(userRole);
    }

    private static void EnsureValidInput(UserId userId, string roleName) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);
    }
}
