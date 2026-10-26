using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Authentication.Contracts.Authentication.Common;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Identity.Application.Authentication.Services;

public sealed class BrowserSessionService(
    IRefreshTokenSessionWriteRepository sessions,
    IUserAuthenticationIdentityService users,
    IJwtTokenGenerator tokens,
    TimeProvider timeProvider) {
    public Task<Result<BrowserSessionModel>> AuthenticateAsync(string? credential, CancellationToken cancellationToken) =>
        AuthenticateCoreAsync(credential, allowStaleVersion: false, cancellationToken);

    public async Task<Result<BrowserSessionModel>> RestoreAsync(string? credential, CancellationToken cancellationToken) {
        Result<BrowserSessionModel> result = await AuthenticateCoreAsync(credential, allowStaleVersion: true, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure) {
            return result;
        }
        bool restored = await sessions.TryRevalidateBrowserSessionAsync(
            new RefreshTokenSessionId(result.Value.SessionId), result.Value.Principal.UserId,
            SecurityTokenGenerator.HashForStorage(credential!), result.Value.Principal.SecurityVersion,
            timeProvider.GetUtcNow().UtcDateTime, cancellationToken).ConfigureAwait(false);
        if (!restored) {
            return Result.Failure<BrowserSessionModel>(AuthenticationErrors.InvalidToken);
        }
        return result;
    }

    private async Task<Result<BrowserSessionModel>> AuthenticateCoreAsync(string? credential, bool allowStaleVersion, CancellationToken cancellationToken) {
        if (!BrowserSessionCredential.TryReadSessionId(credential, out Guid sessionId)) {
            return Result.Failure<BrowserSessionModel>(AuthenticationErrors.InvalidToken);
        }
        UserRefreshTokenSession? session = await sessions.GetByIdAsync(new RefreshTokenSessionId(sessionId), cancellationToken).ConfigureAwait(false);
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        if (session?.IsActive != true || !session.BrowserSecurityVersion.HasValue ||
            session.ExpiresAtUtc is not { } expires || expires <= now ||
            !SecurityTokenGenerator.VerifyFastStorageHash(credential!, session.RefreshTokenHash)) {
            return Result.Failure<BrowserSessionModel>(AuthenticationErrors.InvalidToken);
        }
        Result<UserAuthenticationPrincipalModel> principal = await users.GetAuthenticationPrincipalAsync(session.UserId, now, cancellationToken).ConfigureAwait(false);
        if (principal.IsFailure || (!allowStaleVersion && principal.Value.SecurityVersion != session.BrowserSecurityVersion.Value)) {
            return Result.Failure<BrowserSessionModel>(AuthenticationErrors.InvalidToken);
        }
        return Result.Success(new BrowserSessionModel(credential!, sessionId, expires, session.RememberMe, principal.Value));
    }

    public Task<Result<BrowserSessionModel>> CreateAsync(string refreshToken, string? existingCredential, CancellationToken cancellationToken) =>
        CreateAsync(refreshToken, existingCredential, maximumExpiresAtUtc: null, cancellationToken);

    public async Task<Result<BrowserSessionModel>> CreateAsync(string refreshToken, string? existingCredential, DateTime? maximumExpiresAtUtc, CancellationToken cancellationToken) {
        (UserId userId, string? email, bool rememberMe, RefreshTokenSessionId? refreshSessionId)? proof = tokens.ValidateToken(refreshToken);
        if (proof is not { refreshSessionId: { } sessionId }) {
            return Result.Failure<BrowserSessionModel>(AuthenticationErrors.InvalidToken);
        }
        UserRefreshTokenSession? session = await sessions.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        if (session is null || session.UserId != proof.Value.userId || !session.IsActive || session.BrowserSecurityVersion.HasValue ||
            session.ExpiresAtUtc is not { } expires || expires <= now || maximumExpiresAtUtc <= now ||
            !SecurityTokenGenerator.VerifyFastStorageHash(refreshToken, session.RefreshTokenHash)) {
            return Result.Failure<BrowserSessionModel>(AuthenticationErrors.InvalidToken);
        }
        Result<UserAuthenticationPrincipalModel> principal = await users.GetAuthenticationPrincipalAsync(session.UserId, now, cancellationToken).ConfigureAwait(false);
        if (principal.IsFailure) {
            return Result.Failure<BrowserSessionModel>(AuthenticationErrors.InvalidToken);
        }
        string credential = BrowserSessionCredential.Create(sessionId.Value);
        session.ConvertToBrowserSession(SecurityTokenGenerator.HashForStorage(credential), principal.Value.SecurityVersion, now, maximumExpiresAtUtc);
        await sessions.UpdateAsync(session, cancellationToken).ConfigureAwait(false);
        if (BrowserSessionCredential.TryReadSessionId(existingCredential, out Guid oldId) && oldId != sessionId.Value) {
            UserRefreshTokenSession? previous = await sessions.GetByIdAsync(new RefreshTokenSessionId(oldId), cancellationToken).ConfigureAwait(false);
            if (previous is { IsActive: true, BrowserSecurityVersion: not null } &&
                SecurityTokenGenerator.VerifyFastStorageHash(existingCredential!, previous.RefreshTokenHash)) {
                previous.Revoke(now);
                await sessions.UpdateAsync(previous, cancellationToken).ConfigureAwait(false);
            }
        }
        return Result.Success(new BrowserSessionModel(credential, sessionId.Value, session.ExpiresAtUtc ?? expires, session.RememberMe, principal.Value));
    }

    public async Task<Result> LogoutAsync(string? credential, CancellationToken cancellationToken) {
        if (!BrowserSessionCredential.TryReadSessionId(credential, out Guid id)) {
            return Result.Success();
        }
        UserRefreshTokenSession? session = await sessions.GetByIdAsync(new RefreshTokenSessionId(id), cancellationToken).ConfigureAwait(false);
        if (session is { BrowserSecurityVersion: not null } && SecurityTokenGenerator.VerifyFastStorageHash(credential!, session.RefreshTokenHash)) {
            await sessions.RevokeByIdAsync(session.Id, session.UserId,
                timeProvider.GetUtcNow().UtcDateTime, cancellationToken).ConfigureAwait(false);
        }
        return Result.Success();
    }
}
