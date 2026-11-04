using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;

public interface IAdminSsoSessionLock {
    Task<bool> LockActiveForAdminSsoAsync(
        UserId userId, RefreshTokenSessionId sessionId, CancellationToken cancellationToken = default);
}
