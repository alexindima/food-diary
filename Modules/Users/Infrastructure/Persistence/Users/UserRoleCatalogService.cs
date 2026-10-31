using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Users.Infrastructure.Persistence.Users;

internal sealed class UserRoleCatalogService(DbSet<Role> roleSet, Func<CancellationToken, Task>? synchronizeTransactionAsync = null) : IUserRoleCatalogService {
    public async Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        return await roleSet
            .Where(role => names.Contains(role.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> EnsureRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        IReadOnlyList<Role> existingRoles = await GetRolesByNamesAsync(names, cancellationToken);
        var existingNames = existingRoles.Select(role => role.Name).ToHashSet(StringComparer.Ordinal);
        List<Role> roles = [.. existingRoles];

        foreach (string roleName in names.Where(name => !existingNames.Contains(name))) {
            var role = Role.Create(roleName);
            roles.Add(role);
            await roleSet.AddAsync(role, cancellationToken);
        }

        return roles;
    }
}
