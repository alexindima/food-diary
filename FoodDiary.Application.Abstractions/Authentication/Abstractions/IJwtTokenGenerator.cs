using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Authentication.Abstractions;

public interface IJwtTokenGenerator {
    string GenerateAccessToken(UserId userId, string email, IReadOnlyCollection<string> roles);
    string GenerateAccessToken(
        UserId userId,
        string email,
        IReadOnlyCollection<string> roles,
        DateTime? expiresAtUtc);
    string GenerateAccessToken(
        UserId userId,
        string email,
        IReadOnlyCollection<string> roles,
        JwtImpersonationContext impersonation);
    string GenerateRefreshToken(
        UserId userId,
        string email,
        IReadOnlyCollection<string> roles,
        bool rememberMe = false,
        Guid? refreshSessionId = null);
    (UserId userId, string email, bool rememberMe, Guid? refreshSessionId)? ValidateToken(string token);
}
