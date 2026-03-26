using FoodDiary.Application.Authentication.Abstractions;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Authentication.Services;

public sealed class AuthenticationTokenService(
    IUserRepository userRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    IPasswordHasher passwordHasher)
    : IAuthenticationTokenService {
    public async Task<IssuedAuthenticationTokens> IssueAndStoreAsync(User user, CancellationToken cancellationToken) {
        var roles = GetRoles(user);
        var accessToken = jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, roles);
        var refreshToken = jwtTokenGenerator.GenerateRefreshToken(user.Id, user.Email, roles);

        var hashedRefreshToken = passwordHasher.Hash(refreshToken);
        user.UpdateRefreshToken(hashedRefreshToken);
        await userRepository.UpdateAsync(user, cancellationToken);

        return new IssuedAuthenticationTokens(accessToken, refreshToken);
    }

    public string IssueAccessToken(User user) {
        var roles = GetRoles(user);
        return jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, roles);
    }

    private static string[] GetRoles(User user) {
        return user.UserRoles
            .Select(role => role.Role.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name)
            .ToArray();
    }
}
