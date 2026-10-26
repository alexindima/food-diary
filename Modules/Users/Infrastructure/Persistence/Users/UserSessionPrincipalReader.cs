using FoodDiary.Authentication.Contracts.Authentication.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Users.Infrastructure.Persistence.Users;

public sealed class UserSessionPrincipalReader(DbSet<User> users, Func<CancellationToken, Task>? synchronizeTransactionAsync = null) : IUserSessionPrincipalReader {
    public async Task<Result<UserSessionPrincipalModel>> GetCurrentAsync(UserId userId, DateTime evaluatedAtUtc, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        var data = await users.AsNoTracking().Where(user => user.Id == userId && user.IsActive && user.DeletedAt == null)
            .Select(user => new {
                user.Id,
                user.Email,
                user.SecurityVersion,
                user.PremiumTrialStartedAtUtc,
                user.PremiumTrialEndsAtUtc,
                Roles = user.UserRoles.Select(membership => membership.Role.Name).ToList(),
            }).AsSingleQuery().SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (data is null) {
            return Result.Failure<UserSessionPrincipalModel>(AuthenticationErrors.InvalidToken);
        }
        (IReadOnlyCollection<string> roles, DateTime? cap) = UserAuthenticationPrivileges.Evaluate(
            data.Roles, data.PremiumTrialStartedAtUtc, data.PremiumTrialEndsAtUtc, evaluatedAtUtc);
        return Result.Success(new UserSessionPrincipalModel(data.Id, data.Email, roles, cap, data.SecurityVersion));
    }
}
