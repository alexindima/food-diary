using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Authentication.Abstractions;

public interface IJwtTokenGenerator {
    string GenerateAccessToken(UserId userId, string email, IReadOnlyCollection<string> roles);
    string GenerateAccessToken(
        UserId userId,
        string email,
        IReadOnlyCollection<string> roles,
        JwtImpersonationContext impersonation);
    string GenerateRefreshToken(UserId userId, string email, IReadOnlyCollection<string> roles);
    (UserId userId, string email)? ValidateToken(string token);
}

public sealed record JwtImpersonationContext(UserId ActorUserId, string Reason);
