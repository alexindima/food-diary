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
        await context.Database.ExecuteSqlInterpolatedAsync(sql, cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveRoleAsync(UserId userId, string roleName, CancellationToken cancellationToken = default) {
        EnsureValidInput(userId, roleName);

        string normalizedRoleName = roleName.Trim();
        if (!context.Database.IsRelational()) {
            await RemoveRoleWithTrackedEntitiesAsync(userId, normalizedRoleName, cancellationToken).ConfigureAwait(false);
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
        await context.Database.ExecuteSqlInterpolatedAsync(sql, cancellationToken).ConfigureAwait(false);
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
        await AdvanceTrackedUserSecurityVersionAsync(userId, cancellationToken).ConfigureAwait(false);
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
        await AdvanceTrackedUserSecurityVersionAsync(userId, cancellationToken).ConfigureAwait(false);
    }

    private async Task AdvanceTrackedUserSecurityVersionAsync(UserId userId, CancellationToken cancellationToken) {
        User? user = await context.Users
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken).ConfigureAwait(false);
        user?.RecordExternalRoleMembershipChange();
    }

    private static void EnsureValidInput(UserId userId, string roleName) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);
    }
}
