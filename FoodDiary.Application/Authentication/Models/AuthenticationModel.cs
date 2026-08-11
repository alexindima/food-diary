using FoodDiary.Application.Abstractions.Users.Models;

namespace FoodDiary.Application.Authentication.Models;

public sealed record AuthenticationModel(
    string AccessToken,
    string RefreshToken,
    UserModel User);
