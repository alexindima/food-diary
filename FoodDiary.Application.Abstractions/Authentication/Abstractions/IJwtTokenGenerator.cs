using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Authentication.Abstractions;

public interface IJwtTokenGenerator {
    string GenerateAccessToken(UserId userId, string email, IReadOnlyCollection<string> roles);
    string GenerateRefreshToken(UserId userId, string email, IReadOnlyCollection<string> roles);
    (UserId userId, string email)? ValidateToken(string token);
}
