using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;

public interface IJwtTokenGenerator {
    string GenerateAccessToken(
        UserId userId,
        string? email,
        IReadOnlyCollection<string> roles,
        long securityVersion = 0);
    string GenerateAccessToken(
        UserId userId,
        string? email,
        IReadOnlyCollection<string> roles,
        DateTime? expiresAtUtc,
        long securityVersion = 0);
    string GenerateAccessToken(
        UserId userId,
        string? email,
        IReadOnlyCollection<string> roles,
        DateTime? expiresAtUtc,
        long securityVersion,
        RefreshTokenSessionId refreshSessionId) =>
        GenerateAccessToken(userId, email, roles, expiresAtUtc, securityVersion);
    string GenerateAccessToken(
        UserId userId,
        string? email,
        IReadOnlyCollection<string> roles,
        JwtImpersonationContext impersonation,
        long securityVersion = 0);
    string GenerateRefreshToken(
        UserId userId,
        string? email,
        IReadOnlyCollection<string> roles,
        bool rememberMe = false,
        RefreshTokenSessionId? refreshSessionId = null);
    (UserId userId, string? email, bool rememberMe, RefreshTokenSessionId? refreshSessionId)? ValidateToken(string token);
}
