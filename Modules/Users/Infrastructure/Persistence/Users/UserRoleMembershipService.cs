using Microsoft.EntityFrameworkCore.Infrastructure;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Users.Infrastructure.Persistence.Users;

internal sealed class UserRoleMembershipService(DatabaseFacade database, DbSet<User> users, DbSet<Role> roles, DbSet<UserRole> userRoles, Func<CancellationToken, Task>? synchronizeTransactionAsync = null) : IUserRoleMembershipService {
    public async Task SetPremiumAccessEndAsync(UserId userId, DateTime? endsAtUtc, CancellationToken cancellationToken = default) {
        EnsureValidInput(userId, "Premium");
        if (endsAtUtc is { Kind: not DateTimeKind.Utc }) {
            throw new ArgumentException("Premium expiration must be UTC.", nameof(endsAtUtc));
        }
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        if (!database.IsRelational()) {
            User? user = await users.FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
            user?.SetPremiumAccessEnd(endsAtUtc);
            return;
        }
        await database.ExecuteSqlInterpolatedAsync($"""
            UPDATE "Users"
            SET "PremiumAccessEndsAtUtc" = {endsAtUtc},
                "SecurityVersion" = "SecurityVersion" + CASE
                    WHEN CAST({endsAtUtc} AS timestamptz) IS NOT NULL AND ("PremiumAccessEndsAtUtc" IS NULL OR "PremiumAccessEndsAtUtc" > {endsAtUtc}) THEN 1 ELSE 0 END
            WHERE "Id" = {userId.Value} AND "DeletedAt" IS NULL
              AND "PremiumAccessEndsAtUtc" IS DISTINCT FROM {endsAtUtc}
            """, cancellationToken);
    }

    public async Task EnsureRoleAsync(UserId userId, string roleName, CancellationToken cancellationToken = default) {
        EnsureValidInput(userId, roleName);
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }

        if (!database.IsRelational()) {
            await EnsureRoleWithTrackedEntitiesAsync(userId, roleName.Trim(), cancellationToken);
            return;
        }

        FormattableString sql = $"""
            WITH inserted_role AS (
                INSERT INTO "UserRoles" ("UserId", "RoleId")
                SELECT {userId.Value}, "Id"
                FROM "Roles"
                WHERE "Name" = {roleName.Trim()}
                ON CONFLICT DO NOTHING
                RETURNING 1
            )
            UPDATE "Users"
            SET "SecurityVersion" = "SecurityVersion" + 1
            WHERE "Id" = {userId.Value}
              AND EXISTS (SELECT 1 FROM inserted_role)
            """;
        await database.ExecuteSqlInterpolatedAsync(sql, cancellationToken);
    }

    public async Task RemoveRoleAsync(UserId userId, string roleName, CancellationToken cancellationToken = default) {
        EnsureValidInput(userId, roleName);
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }

        string normalizedRoleName = roleName.Trim();
        if (!database.IsRelational()) {
            await RemoveRoleWithTrackedEntitiesAsync(userId, normalizedRoleName, cancellationToken);
            return;
        }

        FormattableString sql = $"""
            WITH deleted_role AS (
                DELETE FROM "UserRoles"
                WHERE "UserId" = {userId.Value}
                  AND "RoleId" IN (
                      SELECT "Id"
                      FROM "Roles"
                      WHERE "Name" = {normalizedRoleName}
                  )
                RETURNING 1
            )
            UPDATE "Users"
            SET "SecurityVersion" = "SecurityVersion" + 1
            WHERE "Id" = {userId.Value}
              AND EXISTS (SELECT 1 FROM deleted_role)
            """;
        await database.ExecuteSqlInterpolatedAsync(sql, cancellationToken);
    }

    private async Task EnsureRoleWithTrackedEntitiesAsync(UserId userId, string roleName, CancellationToken cancellationToken) {
        Role? role = await roles
            .FirstOrDefaultAsync(candidate => candidate.Name == roleName, cancellationToken);
        if (role is null) {
            return;
        }

        bool alreadyAssigned = await userRoles
            .AnyAsync(userRole => userRole.UserId == userId && userRole.RoleId == role.Id, cancellationToken);
        if (alreadyAssigned) {
            return;
        }

        await userRoles.AddAsync(new UserRole(userId, role.Id), cancellationToken);
        await AdvanceTrackedUserSecurityVersionAsync(userId, cancellationToken);
    }

    private async Task RemoveRoleWithTrackedEntitiesAsync(UserId userId, string roleName, CancellationToken cancellationToken) {
        UserRole? userRole = await userRoles
            .FirstOrDefaultAsync(
                candidate => candidate.UserId == userId && candidate.Role.Name == roleName,
                cancellationToken);
        if (userRole is null) {
            return;
        }

        userRoles.Remove(userRole);
        await AdvanceTrackedUserSecurityVersionAsync(userId, cancellationToken);
    }

    private async Task AdvanceTrackedUserSecurityVersionAsync(UserId userId, CancellationToken cancellationToken) {
        User? user = await users
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        user?.RecordExternalRoleMembershipChange();
    }

    private static void EnsureValidInput(UserId userId, string roleName) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        if (userId == SystemContentOwner.Id) {
            throw new InvalidOperationException("System content owner cannot have roles.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);
    }
}
