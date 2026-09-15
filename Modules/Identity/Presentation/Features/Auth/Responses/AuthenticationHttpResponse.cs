using FoodDiary.Presentation.Api.Features.Users.Responses;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;

public sealed record AuthenticationHttpResponse(
    string AccessToken,
    string RefreshToken,
    UserHttpResponse User);
