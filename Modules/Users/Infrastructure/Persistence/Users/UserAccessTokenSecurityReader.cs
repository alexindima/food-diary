using FoodDiary.Domain.Entities.Users;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Users;

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
