using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Common.Interfaces.Authentication;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(UserId userId, string email, IReadOnlyCollection<string> roles);
    string GenerateRefreshToken(UserId userId, string email, IReadOnlyCollection<string> roles);
    (UserId userId, string email)? ValidateToken(string token);
}
