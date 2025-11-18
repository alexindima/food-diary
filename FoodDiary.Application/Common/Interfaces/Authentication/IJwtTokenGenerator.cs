using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Common.Interfaces.Authentication;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(UserId userId, string email);
    string GenerateRefreshToken(UserId userId, string email);
    (UserId userId, string email)? ValidateToken(string token);
}
