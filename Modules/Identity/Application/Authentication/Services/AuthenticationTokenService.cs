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
    TimeProvider dateTimeProvider)
    : IAuthenticationTokenService {
    public async Task<IssuedAuthenticationTokens> IssueFromPrincipalAsync(
        UserAuthenticationPrincipalModel principal,
        CancellationToken cancellationToken,
        AuthenticationClientContext? clientContext = null,
        bool rememberMe = false) {
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
        CancellationToken cancellationToken) {
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
