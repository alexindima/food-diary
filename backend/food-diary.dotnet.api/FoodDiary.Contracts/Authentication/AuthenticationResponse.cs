using FoodDiary.Contracts.Users;

namespace FoodDiary.Contracts.Authentication;

public record AuthenticationResponse(
    string AccessToken,
    string RefreshToken,
    UserResponse User
);
