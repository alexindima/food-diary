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
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Services;

namespace FoodDiary.Modules.Identity.Application.Authentication.Services;

public sealed class BrowserSessionService(
    IRefreshTokenSessionWriteRepository sessions,
    IUserAuthenticationIdentityService users,
    IJwtTokenGenerator tokens,
    TimeProvider timeProvider,
    IUserSessionPrincipalReader principals,
    IAuthenticationSessionLifetimePolicy lifetimes) {
    public async Task<Result<BrowserSessionIdentityModel>> AuthenticateAsync(string? credential, CancellationToken cancellationToken) {
        if (!BrowserSessionCredential.TryReadSessionId(credential, out Guid id)) {
            return Result.Failure<BrowserSessionIdentityModel>(AuthenticationErrors.InvalidToken);
        }
        BrowserSessionProof? proof = await sessions.ReadBrowserSessionProofAsync(new RefreshTokenSessionId(id), cancellationToken).ConfigureAwait(false);
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        if (proof is not { RevokedAtUtc: null, SecurityVersion: not null, ExpiresAtUtc: { } expires } || expires <= now ||
            !SecurityTokenGenerator.VerifyFastStorageHash(credential!, proof.CredentialHash)) {
            return Result.Failure<BrowserSessionIdentityModel>(AuthenticationErrors.InvalidToken);
        }
        Result<UserSessionPrincipalModel> principal = await principals.GetCurrentAsync(proof.UserId, now, cancellationToken).ConfigureAwait(false);
        expires = EffectiveExpiration(expires, proof.CreatedAtUtc, proof.SlidingExpirationEnabled);
        if (expires <= now) {
            return Result.Failure<BrowserSessionIdentityModel>(AuthenticationErrors.InvalidToken);
        }
        if (principal.IsFailure || principal.Value.SecurityVersion != proof.SecurityVersion.Value) {
            return Result.Failure<BrowserSessionIdentityModel>(AuthenticationErrors.InvalidToken);
        }
        DateTime activityCutoff = now.AddMinutes(-5);
        if (proof.LastActiveAtUtc <= activityCutoff) {
            await sessions.TryRecordBrowserActivityAsync(proof, now, activityCutoff, cancellationToken).ConfigureAwait(false);
        }
        return Result.Success(new BrowserSessionIdentityModel(id, expires, principal.Value));
    }

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
        BrowserSessionModel session = result.Value;
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        if (!session.SlidingExpirationEnabled || lifetimes.GetRenewedExpiration(session.CreatedAtUtc, session.ExpiresAtUtc, session.RememberMe, now) is not { } expires) {
            return result;
        }
        var proof = new BrowserSessionProof(new RefreshTokenSessionId(session.SessionId), session.Principal.UserId,
            SecurityTokenGenerator.HashForStorage(credential!), session.Principal.SecurityVersion, session.ExpiresAtUtc,
            RevokedAtUtc: null, now);
        if (await sessions.TryRenewBrowserSessionAsync(proof, now, expires, cancellationToken).ConfigureAwait(false)) {
            return Result.Success(session with { ExpiresAtUtc = expires, Renewed = true });
        }
        BrowserSessionProof? current = await sessions.ReadBrowserSessionProofAsync(proof.Id, cancellationToken).ConfigureAwait(false);
        if (current is { RevokedAtUtc: null, ExpiresAtUtc: { } currentExpires } && currentExpires > now &&
            current.UserId == proof.UserId && current.SecurityVersion == proof.SecurityVersion &&
            SecurityTokenGenerator.VerifyFastStorageHash(credential!, current.CredentialHash)) {
            return Result.Success(session with { ExpiresAtUtc = currentExpires, Renewed = true });
        }
        return Result.Failure<BrowserSessionModel>(AuthenticationErrors.InvalidToken);
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
        expires = EffectiveExpiration(expires, session.CreatedAtUtc, session.BrowserSlidingExpirationEnabled);
        if (expires <= now) {
            return Result.Failure<BrowserSessionModel>(AuthenticationErrors.InvalidToken);
        }
        if (principal.IsFailure || (!allowStaleVersion && principal.Value.SecurityVersion != session.BrowserSecurityVersion.Value)) {
            return Result.Failure<BrowserSessionModel>(AuthenticationErrors.InvalidToken);
        }
        return Result.Success(new BrowserSessionModel(credential!, sessionId, expires, session.RememberMe, principal.Value,
            session.BrowserSlidingExpirationEnabled, session.CreatedAtUtc));
    }

    public Task<Result<BrowserSessionModel>> CreateAsync(string refreshToken, string? existingCredential, CancellationToken cancellationToken) =>
        CreateAsync(refreshToken, existingCredential, maximumExpiresAtUtc: null, cancellationToken);

    private DateTime EffectiveExpiration(DateTime expires, DateTime created, bool renewable) =>
        renewable && lifetimes.GetAbsoluteExpiration(created) is { } maximum && maximum < expires ? maximum : expires;

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
        session.ConvertToBrowserSession(SecurityTokenGenerator.HashForStorage(credential), principal.Value.SecurityVersion, now, maximumExpiresAtUtc,
            allowRenewal: maximumExpiresAtUtc is null && !string.Equals(session.AuthProvider, "admin-sso", StringComparison.Ordinal));
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
