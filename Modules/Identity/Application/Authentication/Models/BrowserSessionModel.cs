using System.Text.Json.Serialization;
using FoodDiary.Modules.Users.Contracts.Models;

namespace FoodDiary.Modules.Identity.Application.Authentication.Models;

public sealed record BrowserSessionModel(
    [property: JsonIgnore] string Credential,
    Guid SessionId,
    DateTime ExpiresAtUtc,
    bool RememberMe,
    UserAuthenticationPrincipalModel Principal) {
    public override string ToString() => $"BrowserSessionModel {{ SessionId = {SessionId}, Credential = [redacted] }}";
}
