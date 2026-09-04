using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Authentication.Abstractions;

public interface IJwtTokenGenerator {
    string GenerateAccessToken(
        UserId userId,
        string email,
        IReadOnlyCollection<string> roles,
        long securityVersion = 0);
    string GenerateAccessToken(
        UserId userId,
        string email,
        IReadOnlyCollection<string> roles,
        DateTime? expiresAtUtc,
        long securityVersion = 0);
    string GenerateAccessToken(
        UserId userId,
        string email,
        IReadOnlyCollection<string> roles,
        DateTime? expiresAtUtc,
        long securityVersion,
        Guid refreshSessionId) =>
        GenerateAccessToken(userId, email, roles, expiresAtUtc, securityVersion);
    string GenerateAccessToken(
        UserId userId,
        string email,
        IReadOnlyCollection<string> roles,
        JwtImpersonationContext impersonation,
        long securityVersion = 0);
    string GenerateRefreshToken(
        UserId userId,
        string email,
        IReadOnlyCollection<string> roles,
        bool rememberMe = false,
        Guid? refreshSessionId = null);
    (UserId userId, string email, bool rememberMe, Guid? refreshSessionId)? ValidateToken(string token);
}
