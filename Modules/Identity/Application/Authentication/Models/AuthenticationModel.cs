using FoodDiary.Modules.Users.Contracts.Models;

namespace FoodDiary.Modules.Identity.Application.Authentication.Models;

public sealed record AuthenticationModel(
    string AccessToken,
    string RefreshToken,
    UserModel User);
