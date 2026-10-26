using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using System.Text.Json.Serialization;

namespace FoodDiary.Modules.Identity.Application.Authentication.Models;

public sealed record AuthenticationModel(
    string AccessToken,
    string RefreshToken,
    UserModel User,
    [property: JsonIgnore] IssuedBrowserSession? BrowserSession = null);
