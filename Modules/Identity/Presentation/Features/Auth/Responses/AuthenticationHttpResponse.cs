using FoodDiary.Modules.Users.Presentation.Contracts.Responses;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;

public sealed record AuthenticationHttpResponse(
    string AccessToken,
    string RefreshToken,
    UserHttpResponse User);
