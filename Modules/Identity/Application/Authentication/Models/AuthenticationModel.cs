using FoodDiary.Application.Abstractions.Users.Models;

namespace FoodDiary.Application.Identity.Authentication.Models;

public sealed record AuthenticationModel(
    string AccessToken,
    string RefreshToken,
    UserModel User);
