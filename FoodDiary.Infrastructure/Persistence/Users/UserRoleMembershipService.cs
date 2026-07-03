using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Users;

internal sealed class UserRoleMembershipService(FoodDiaryDbContext context) : IUserRoleMembershipService {
    public async Task EnsureRoleAsync(UserId userId, string roleName, CancellationToken cancellationToken = default) {
        EnsureValidInput(userId, roleName);

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
        await context.UserRoles
            .Where(userRole => userRole.UserId == userId && userRole.Role.Name == normalizedRoleName)
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void EnsureValidInput(UserId userId, string roleName) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);
    }
}
