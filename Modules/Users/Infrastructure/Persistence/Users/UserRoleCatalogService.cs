using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Users;

internal sealed class UserRoleCatalogService(DbSet<Role> roleSet, Func<CancellationToken, Task>? synchronizeTransactionAsync = null) : IUserRoleCatalogService {
    public async Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        return await roleSet
            .Where(role => names.Contains(role.Name))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Role>> EnsureRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        IReadOnlyList<Role> existingRoles = await GetRolesByNamesAsync(names, cancellationToken).ConfigureAwait(false);
        var existingNames = existingRoles.Select(role => role.Name).ToHashSet(StringComparer.Ordinal);
        List<Role> roles = [.. existingRoles];

        foreach (string roleName in names.Where(name => !existingNames.Contains(name))) {
            var role = Role.Create(roleName);
            roles.Add(role);
            await roleSet.AddAsync(role, cancellationToken).ConfigureAwait(false);
        }

        return roles;
    }
}
