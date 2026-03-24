using FoodDiary.Application.Users.Models;

namespace FoodDiary.Application.Authentication.Models;

public sealed record AuthenticationModel(
    string AccessToken,
    string RefreshToken,
    UserModel User);
