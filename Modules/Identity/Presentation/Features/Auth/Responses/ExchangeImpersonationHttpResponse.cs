using System.Text.Json.Serialization;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;

public sealed record ExchangeImpersonationHttpResponse(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? AccessToken,
    string? ContextId = null);
