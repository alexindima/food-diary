using FoodDiary.Modules.Users.Presentation.Contracts.Responses;
using System.Text.Json.Serialization;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;

public sealed record AuthenticationHttpResponse(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? AccessToken,
    [property: JsonIgnore] string RefreshToken,
    UserHttpResponse User,
    IReadOnlyList<string>? Roles = null,
    string? SessionId = null,
    string? ImpersonationContext = null,
    string? ImpersonationReason = null,
    bool RememberMe = false,
    [property: JsonIgnore] IssuedBrowserSession? BrowserSession = null);
