using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Identity.Authentication.Services.UserAgents;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Identity.Authentication.Services;

public sealed class AuthenticationTokenService(
    IUserLoginEventWriteRepository userLoginEventRepository,
    IRefreshTokenSessionWriteRepository refreshTokenSessionRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    TimeProvider dateTimeProvider)
    : IAuthenticationTokenService {
    public async Task<IssuedAuthenticationTokens> IssueFromPrincipalAsync(
        UserAuthenticationPrincipalModel principal,
        CancellationToken cancellationToken,
        AuthenticationClientContext? clientContext = null,
        bool rememberMe = false) {
        string accessToken = jwtTokenGenerator.GenerateAccessToken(
            principal.UserId,
            principal.Email,
            principal.Roles,
            principal.AccessTokenCapUtc,
            principal.SecurityVersion);
        var resolvedRefreshSessionId = Guid.NewGuid();
        string refreshToken = jwtTokenGenerator.GenerateRefreshToken(
            principal.UserId,
            principal.Email,
            principal.Roles,
            rememberMe,
            resolvedRefreshSessionId);
        string hashedRefreshToken = SecurityTokenGenerator.HashForStorage(refreshToken);
        DateTime nowUtc = dateTimeProvider.GetUtcNow().UtcDateTime;
        await PersistAuthenticationArtifactsAsync(
            principal.UserId,
            hashedRefreshToken,
            rememberMe,
            resolvedRefreshSessionId,
            clientContext,
            nowUtc,
            cancellationToken).ConfigureAwait(false);
        return new IssuedAuthenticationTokens(accessToken, refreshToken);
    }

    public async Task<IssuedAuthenticationTokens?> RotateFromPrincipalAsync(
        UserAuthenticationPrincipalModel principal,
        Guid refreshSessionId,
        string expectedRefreshTokenHash,
        bool rememberMe,
        CancellationToken cancellationToken) {
        string accessToken = jwtTokenGenerator.GenerateAccessToken(
            principal.UserId,
            principal.Email,
            principal.Roles,
            principal.AccessTokenCapUtc,
            principal.SecurityVersion);
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
            cancellationToken).ConfigureAwait(false);
        return rotated ? new IssuedAuthenticationTokens(accessToken, refreshToken) : null;
    }

    private async Task PersistAuthenticationArtifactsAsync(
        UserId userId,
        string hashedRefreshToken,
        bool rememberMe,
        Guid resolvedRefreshSessionId,
        AuthenticationClientContext? clientContext,
        DateTime nowUtc,
        CancellationToken cancellationToken) {
        var session = UserRefreshTokenSession.Create(
            resolvedRefreshSessionId,
            userId,
            hashedRefreshToken,
            rememberMe,
            clientContext?.AuthProvider,
            clientContext?.IpAddress,
            clientContext?.UserAgent,
            nowUtc);
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
