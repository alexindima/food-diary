using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Authentication.Services.UserAgents;

namespace FoodDiary.Application.Authentication.Services;

public sealed class AuthenticationTokenService(
    IUserRepository userRepository,
    IUserLoginEventRepository userLoginEventRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    IDateTimeProvider dateTimeProvider)
    : IAuthenticationTokenService {
    public async Task<IssuedAuthenticationTokens> IssueAndStoreAsync(
        User user,
        CancellationToken cancellationToken,
        AuthenticationClientContext? clientContext = null) {
        var roles = GetRoles(user);
        var accessToken = jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, roles);
        var refreshToken = jwtTokenGenerator.GenerateRefreshToken(user.Id, user.Email, roles);
        var nowUtc = dateTimeProvider.UtcNow;

        var hashedRefreshToken = SecurityTokenGenerator.HashForStorage(refreshToken);
        user.UpdateRefreshToken(new UserRefreshTokenUpdate(
            RefreshToken: hashedRefreshToken,
            ChangedAtUtc: nowUtc));
        await userRepository.UpdateAsync(user, cancellationToken);

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
            await userLoginEventRepository.AddAsync(loginEvent, cancellationToken);
        }

        return new IssuedAuthenticationTokens(accessToken, refreshToken);
    }

    public string IssueAccessToken(User user) {
        var roles = GetRoles(user);
        return jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, roles);
    }

    private static string[] GetRoles(User user) {
        return user.GetRoleNames().ToArray();
    }
}
