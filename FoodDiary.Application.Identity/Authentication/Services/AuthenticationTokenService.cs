using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Authentication.Services.UserAgents;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Authentication.Services;

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
        bool rememberMe = false,
        Guid? refreshSessionId = null) {
        string accessToken = jwtTokenGenerator.GenerateAccessToken(
            principal.UserId,
            principal.Email,
            principal.Roles,
            principal.AccessTokenCapUtc);
        Guid resolvedRefreshSessionId = refreshSessionId ?? Guid.NewGuid();
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
            refreshSessionId,
            clientContext,
            nowUtc,
            cancellationToken).ConfigureAwait(false);
        return new IssuedAuthenticationTokens(accessToken, refreshToken);
    }

    private async Task PersistAuthenticationArtifactsAsync(
        UserId userId,
        string hashedRefreshToken,
        bool rememberMe,
        Guid resolvedRefreshSessionId,
        Guid? refreshSessionId,
        AuthenticationClientContext? clientContext,
        DateTime nowUtc,
        CancellationToken cancellationToken) {
        if (refreshSessionId.HasValue) {
            UserRefreshTokenSession? session = await refreshTokenSessionRepository
                .GetByIdAsync(refreshSessionId.Value, cancellationToken)
                .ConfigureAwait(false);
            if (session is not null && session.UserId == userId && session.IsActive) {
                session.Rotate(hashedRefreshToken, rememberMe, nowUtc, TimeSpan.Zero);
                await refreshTokenSessionRepository.UpdateAsync(session, cancellationToken).ConfigureAwait(false);
            }
        } else {
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
        }

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
