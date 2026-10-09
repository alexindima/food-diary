using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Identity.Infrastructure.Persistence.Users;

public sealed class AccessTokenSessionReader(IRefreshTokenSessionReadModelRepository sessions) : IUserAccessTokenSessionReader {
    public Task<bool> IsActiveAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default) =>
        sessions.IsActiveAsync(new UserId(userId), new RefreshTokenSessionId(sessionId), cancellationToken);
}
