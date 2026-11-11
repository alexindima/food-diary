using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Identity.Application.Authentication.Services;

public sealed class PrivilegedSessionService(IRefreshTokenSessionReadRepository sessions, TimeProvider clock) {
    public static readonly TimeSpan ConfirmationLifetime = TimeSpan.FromMinutes(15);

    public async Task<bool> IsConfirmedAsync(UserId userId, Guid sessionId, bool allowAdminSession, CancellationToken cancellationToken) {
        UserRefreshTokenSession? session = await sessions.GetByIdAsync(new RefreshTokenSessionId(sessionId), cancellationToken);
        DateTime now = clock.GetUtcNow().UtcDateTime;
        if (session is null || session.UserId != userId || !session.IsActive || session.ExpiresAtUtc is not { } expires || expires <= now) {
            return false;
        }
        if (allowAdminSession && string.Equals(session.AuthProvider, "admin-sso", StringComparison.Ordinal)) {
            return session.AdminAccessExpiresAtUtc > now;
        }
        return session.AuthProvider is "password" or "google" && session.CreatedAtUtc <= now &&
            now - session.CreatedAtUtc < ConfirmationLifetime;
    }
}
