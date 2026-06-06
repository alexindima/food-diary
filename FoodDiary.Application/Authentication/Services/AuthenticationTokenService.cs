using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Authentication.Services.UserAgents;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Authentication.Services;

public sealed class AuthenticationTokenService(
    IUserRepository userRepository,
    IUserLoginEventRepository userLoginEventRepository,
    IRefreshTokenSessionRepository refreshTokenSessionRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    IDateTimeProvider dateTimeProvider)
    : IAuthenticationTokenService {
    public async Task<IssuedAuthenticationTokens> IssueAndStoreAsync(
        User user,
        CancellationToken cancellationToken,
        AuthenticationClientContext? clientContext = null,
        bool rememberMe = false,
        Guid? refreshSessionId = null) {
        var roles = GetRoles(user);
        var accessToken = jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, roles, ResolveAccessTokenCapUtc(user));
        var resolvedRefreshSessionId = refreshSessionId ?? Guid.NewGuid();
        var refreshToken = jwtTokenGenerator.GenerateRefreshToken(user.Id, user.Email, roles, rememberMe, resolvedRefreshSessionId);
        var nowUtc = dateTimeProvider.UtcNow;

        var hashedRefreshToken = SecurityTokenGenerator.HashForStorage(refreshToken);
        user.RecordAuthenticationActivity(nowUtc);
        await userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);

        if (refreshSessionId.HasValue) {
            var session = await refreshTokenSessionRepository.GetByIdAsync(refreshSessionId.Value, cancellationToken).ConfigureAwait(false);
            if (session is not null && session.UserId == user.Id && session.IsActive) {
                session.Rotate(hashedRefreshToken, rememberMe, nowUtc);
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
            var userAgent = UserAgentParser.Parse(clientContext.UserAgent);
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
        var roles = GetRoles(user);
        return jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, roles, ResolveAccessTokenCapUtc(user));
    }

    private string[] GetRoles(User user) {
        var roles = user.GetRoleNames().ToList();
        if (user.HasActivePremiumTrial(dateTimeProvider.UtcNow) &&
            !roles.Contains(RoleNames.Premium, StringComparer.Ordinal)) {
            roles.Add(RoleNames.Premium);
        }

        return roles.ToArray();
    }

    private DateTime? ResolveAccessTokenCapUtc(User user) {
        if (user.HasRole(RoleNames.Premium) ||
            !user.HasActivePremiumTrial(dateTimeProvider.UtcNow)) {
            return null;
        }

        return user.PremiumTrialEndsAtUtc;
    }
}
