using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Authentication.Services.UserAgents;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Authentication.Services;

public sealed class AuthenticationTokenService(
    IAuthenticationUserMutationService userMutationService,
    IUserLoginEventWriteRepository userLoginEventRepository,
    IRefreshTokenSessionWriteRepository refreshTokenSessionRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    TimeProvider dateTimeProvider)
    : IAuthenticationTokenService {
    private static readonly TimeSpan PreviousRefreshTokenGracePeriod = TimeSpan.FromMinutes(2);

    public async Task<IssuedAuthenticationTokens> IssueAndStoreAsync(
        User user,
        CancellationToken cancellationToken,
        AuthenticationClientContext? clientContext = null,
        bool rememberMe = false,
        Guid? refreshSessionId = null) {
        string[] roles = GetRoles(user);
        string accessToken = jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, roles, ResolveAccessTokenCapUtc(user));
        Guid resolvedRefreshSessionId = refreshSessionId ?? Guid.NewGuid();
        string refreshToken = jwtTokenGenerator.GenerateRefreshToken(user.Id, user.Email, roles, rememberMe, resolvedRefreshSessionId);
        DateTime nowUtc = dateTimeProvider.GetUtcNow().UtcDateTime;

        string hashedRefreshToken = SecurityTokenGenerator.HashForStorage(refreshToken);
        user.RecordAuthenticationActivity(nowUtc);
        await userMutationService.UpdateAsync(user, cancellationToken).ConfigureAwait(false);

        if (refreshSessionId.HasValue) {
            UserRefreshTokenSession? session = await refreshTokenSessionRepository.GetByIdAsync(refreshSessionId.Value, cancellationToken).ConfigureAwait(false);
            if (session is not null && session.UserId == user.Id && session.IsActive) {
                session.Rotate(hashedRefreshToken, rememberMe, nowUtc, PreviousRefreshTokenGracePeriod);
                await refreshTokenSessionRepository.UpdateAsync(session, cancellationToken).ConfigureAwait(false);
            }
        } else {
            var session = UserRefreshTokenSession.Create(
                resolvedRefreshSessionId,
                user.Id,
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
                user.Id,
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

        return new IssuedAuthenticationTokens(accessToken, refreshToken);
    }

    public string IssueAccessToken(User user) {
        string[] roles = GetRoles(user);
        return jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, roles, ResolveAccessTokenCapUtc(user));
    }

    private string[] GetRoles(User user) {
        var roles = user.GetRoleNames().ToList();
        if (user.HasActivePremiumTrial(dateTimeProvider.GetUtcNow().UtcDateTime) &&
            !roles.Contains(RoleNames.Premium, StringComparer.Ordinal)) {
            roles.Add(RoleNames.Premium);
        }

        return [.. roles];
    }

    private DateTime? ResolveAccessTokenCapUtc(User user) {
        if (user.HasRole(RoleNames.Premium) ||
            !user.HasActivePremiumTrial(dateTimeProvider.GetUtcNow().UtcDateTime)) {
            return null;
        }

        return user.PremiumTrialEndsAtUtc;
    }
}
