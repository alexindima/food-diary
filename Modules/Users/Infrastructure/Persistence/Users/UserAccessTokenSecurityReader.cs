using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Users;

public sealed class UserAccessTokenSecurityReader(FoodDiaryDbContext context) : IUserAccessTokenSecurityReader {
    public Task<bool> IsCurrentAsync(
        Guid userId,
        long securityVersion,
        CancellationToken cancellationToken = default) =>
        context.Users.AsNoTracking().AnyAsync(
            user => user.Id == new UserId(userId) &&
                    user.IsActive &&
                    user.DeletedAt == null &&
                    user.SecurityVersion == securityVersion,
            cancellationToken);
}
