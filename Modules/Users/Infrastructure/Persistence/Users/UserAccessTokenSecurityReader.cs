using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Users.Infrastructure.Persistence.Users;

public sealed class UserAccessTokenSecurityReader(DbSet<User> users, Func<CancellationToken, Task>? synchronizeTransactionAsync = null) : IUserAccessTokenSecurityReader {
    public async Task<bool> IsCurrentAsync(
        Guid userId,
        long securityVersion,
        CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        return await users.AsNoTracking().AnyAsync(
            user => user.Id == new UserId(userId) &&
                    user.IsActive &&
                    user.DeletedAt == null &&
                    user.SecurityVersion == securityVersion,
            cancellationToken).ConfigureAwait(false);
    }
}
