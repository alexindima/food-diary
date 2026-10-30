using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Services;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Services.UserAgents;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Authentication.Contracts.Authentication.Common;

namespace FoodDiary.Modules.Identity.Application.Authentication.Services;

public sealed class AuthenticationTokenService(
    IUserLoginEventWriteRepository userLoginEventRepository,
    IRefreshTokenSessionWriteRepository refreshTokenSessionRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    IPasswordHasher passwordHasher,
    TimeProvider dateTimeProvider,
    IAuthenticationSessionLifetimePolicy lifetimes)
    : IAuthenticationTokenService {
    public async Task<IssuedAuthenticationTokens> IssueFromPrincipalAsync(
        UserAuthenticationPrincipalModel principal,
        CancellationToken cancellationToken,
        AuthenticationClientContext? clientContext = null,
        bool rememberMe = false) {
        if (clientContext is { CredentialKind: not AuthenticationCredentialKind.Bearer }) {
            return await IssueBrowserAsync(principal, clientContext, rememberMe, cancellationToken).ConfigureAwait(false);
        }
        var resolvedRefreshSessionId = RefreshTokenSessionId.New();
        string accessToken = jwtTokenGenerator.GenerateAccessToken(
            principal.UserId,
            principal.Email,
            principal.Roles,
            principal.AccessTokenCapUtc,
            principal.SecurityVersion,
            resolvedRefreshSessionId);
        string refreshToken = jwtTokenGenerator.GenerateRefreshToken(
            principal.UserId,
            principal.Email,
            principal.Roles,
            rememberMe,
            resolvedRefreshSessionId);
        string hashedRefreshToken = SecurityTokenGenerator.HashForStorage(refreshToken);
        DateTime nowUtc = dateTimeProvider.GetUtcNow().UtcDateTime;
        await RevokeExistingBrowserSessionAsync(principal.UserId, clientContext?.ExistingRefreshToken, nowUtc, cancellationToken)
            .ConfigureAwait(false);
        await PersistAuthenticationArtifactsAsync(
            principal.UserId,
            hashedRefreshToken,
            rememberMe,
            resolvedRefreshSessionId,
            clientContext,
            nowUtc,
            jwtTokenGenerator.ReadRefreshTokenExpirationUtc(refreshToken),
            cancellationToken).ConfigureAwait(false);
        return new IssuedAuthenticationTokens(accessToken, refreshToken);
    }

    private async Task<IssuedAuthenticationTokens> IssueBrowserAsync(
        UserAuthenticationPrincipalModel principal, AuthenticationClientContext client,
        bool rememberMe, CancellationToken cancellationToken) {
        DateTime now = dateTimeProvider.GetUtcNow().UtcDateTime;
        bool admin = client.CredentialKind == AuthenticationCredentialKind.AdminBrowser;
        rememberMe |= !admin && client.AuthProvider is "google" or "telegram" or "telegram-login-widget" or "telegram-mini-app";
        DateTime expires = now.Add(lifetimes.GetLifetime(client.CredentialKind, rememberMe));
        if (admin && principal.AccessTokenCapUtc is { } cap && cap < expires) {
            expires = cap;
        }
        var id = RefreshTokenSessionId.New();
        string credential = BrowserSessionCredential.Create(id.Value);
        if (client.PreviousBrowserSessionId is { } previousId) {
            // The protected marker selects a previous browser slot only after fresh authentication.
            // Revoke before loading the old proof so a staged entity cannot overwrite this atomic update.
            await refreshTokenSessionRepository.RevokeByIdAsync(previousId, principal.UserId, now, cancellationToken).ConfigureAwait(false);
        }
        await RevokeExistingBrowserSessionAsync(principal.UserId, client.ExistingRefreshToken, now, cancellationToken).ConfigureAwait(false);
        await RevokeExistingOpaqueSessionAsync(client.ExistingBrowserCredential, now, cancellationToken).ConfigureAwait(false);
        await PersistAuthenticationArtifactsAsync(principal.UserId, SecurityTokenGenerator.HashForStorage(credential),
            rememberMe, id, client, now, expires, cancellationToken, principal.SecurityVersion).ConfigureAwait(false);
        return new IssuedAuthenticationTokens(string.Empty, string.Empty,
            new IssuedBrowserSession(credential, id.Value, expires, rememberMe, principal.Roles, admin));
    }

    private async Task RevokeExistingOpaqueSessionAsync(string? credential, DateTime nowUtc, CancellationToken cancellationToken) {
        if (!BrowserSessionCredential.TryReadSessionId(credential, out Guid id)) {
            return;
        }
        UserRefreshTokenSession? previous = await refreshTokenSessionRepository.GetByIdAsync(new RefreshTokenSessionId(id), cancellationToken).ConfigureAwait(false);
        if (previous is { IsActive: true, BrowserSecurityVersion: not null } &&
            SecurityTokenGenerator.VerifyFastStorageHash(credential!, previous.RefreshTokenHash)) {
            previous.Revoke(nowUtc);
            await refreshTokenSessionRepository.UpdateAsync(previous, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IssuedAuthenticationTokens?> RotateFromPrincipalAsync(
        UserAuthenticationPrincipalModel principal,
        RefreshTokenSessionId refreshSessionId,
        string expectedRefreshTokenHash,
        bool rememberMe,
        CancellationToken cancellationToken) {
        string accessToken = jwtTokenGenerator.GenerateAccessToken(
            principal.UserId,
            principal.Email,
            principal.Roles,
            principal.AccessTokenCapUtc,
            principal.SecurityVersion,
            refreshSessionId);
        string refreshToken = jwtTokenGenerator.GenerateRefreshToken(
            principal.UserId,
            principal.Email,
            principal.Roles,
            rememberMe,
            refreshSessionId);
        string newRefreshTokenHash = SecurityTokenGenerator.HashForStorage(refreshToken);
        bool rotated = await refreshTokenSessionRepository.TryRotateAsync(
            refreshSessionId,
            principal.UserId,
            expectedRefreshTokenHash,
            newRefreshTokenHash,
            rememberMe,
            dateTimeProvider.GetUtcNow().UtcDateTime,
            jwtTokenGenerator.ReadRefreshTokenExpirationUtc(refreshToken),
            cancellationToken).ConfigureAwait(false);
        return rotated ? new IssuedAuthenticationTokens(accessToken, refreshToken) : null;
    }

    private async Task RevokeExistingBrowserSessionAsync(
        UserId userId,
        string? existingRefreshToken,
        DateTime nowUtc,
        CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(existingRefreshToken)) {
            return;
        }

        (UserId userId, string? email, bool rememberMe, RefreshTokenSessionId? refreshSessionId)? validation =
            jwtTokenGenerator.ValidateToken(existingRefreshToken);
        if (validation is not { refreshSessionId: { } sessionId } || validation.Value.userId != userId) {
            return;
        }

        UserRefreshTokenSession? session = await refreshTokenSessionRepository.GetByIdAsync(sessionId, cancellationToken)
            .ConfigureAwait(false);
        if (session is null || session.UserId != userId || !session.IsActive) {
            return;
        }

        bool matches = SecurityTokenGenerator.IsFastStorageHash(session.RefreshTokenHash)
            ? SecurityTokenGenerator.VerifyFastStorageHash(existingRefreshToken, session.RefreshTokenHash)
            : passwordHasher.Verify(SecurityTokenGenerator.NormalizeForSecureHashing(existingRefreshToken), session.RefreshTokenHash);
        if (matches) {
            // Stage both replacement and revocation in the same unit-of-work save.
            session.Revoke(nowUtc);
            await refreshTokenSessionRepository.UpdateAsync(session, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task PersistAuthenticationArtifactsAsync(
        UserId userId,
        string hashedRefreshToken,
        bool rememberMe,
        RefreshTokenSessionId resolvedRefreshSessionId,
        AuthenticationClientContext? clientContext,
        DateTime nowUtc,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken,
        long? browserSecurityVersion = null) {
        var session = UserRefreshTokenSession.Create(
            resolvedRefreshSessionId,
            userId,
            hashedRefreshToken,
            rememberMe,
            clientContext?.AuthProvider,
            clientContext?.IpAddress,
            clientContext?.UserAgent,
            nowUtc,
            expiresAtUtc);
        if (browserSecurityVersion is { } version) {
            session.ConvertToBrowserSession(hashedRefreshToken, version, nowUtc,
                allowRenewal: clientContext?.CredentialKind == AuthenticationCredentialKind.Browser);
        }
        await refreshTokenSessionRepository.AddAsync(session, cancellationToken).ConfigureAwait(false);

        if (clientContext is not null) {
            ParsedUserAgent userAgent = UserAgentParser.Parse(clientContext.UserAgent);
            var loginEvent = UserLoginEvent.Create(
                userId,
                clientContext.AuthProvider,
                clientContext.IpAddress,
                clientContext.UserAgent,
                userAgent.BrowserName,
                userAgent.BrowserVersion,
                userAgent.OperatingSystem,
                userAgent.DeviceType,
                nowUtc);
            await userLoginEventRepository.AddAsync(loginEvent, cancellationToken).ConfigureAwait(false);
        }
    }
}
