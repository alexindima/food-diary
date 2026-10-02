using FoodDiary.Modules.Users.Presentation.Contracts.Responses;
using System.Text.Json.Serialization;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;

public sealed record AuthenticationHttpResponse(
    string AccessToken,
    [property: JsonIgnore] string RefreshToken,
    UserHttpResponse User);
