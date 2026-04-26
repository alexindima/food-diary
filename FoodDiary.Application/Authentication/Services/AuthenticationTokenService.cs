using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Authentication.Services;

public sealed class AuthenticationTokenService(
    IUserRepository userRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    IDateTimeProvider dateTimeProvider)
    : IAuthenticationTokenService {
    public async Task<IssuedAuthenticationTokens> IssueAndStoreAsync(User user, CancellationToken cancellationToken) {
        var roles = GetRoles(user);
        var accessToken = jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, roles);
        var refreshToken = jwtTokenGenerator.GenerateRefreshToken(user.Id, user.Email, roles);

        var hashedRefreshToken = SecurityTokenGenerator.HashForStorage(refreshToken);
        user.UpdateRefreshToken(new UserRefreshTokenUpdate(
            RefreshToken: hashedRefreshToken,
            ChangedAtUtc: dateTimeProvider.UtcNow));
        await userRepository.UpdateAsync(user, cancellationToken);

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
