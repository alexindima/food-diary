using System.Text.Json.Serialization;

namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;

public sealed record IssuedBrowserSession(
    [property: JsonIgnore] string Credential,
    Guid SessionId,
    DateTime ExpiresAtUtc,
    bool RememberMe,
    IReadOnlyCollection<string> Roles,
    bool IsAdmin) {
    public override string ToString() => $"IssuedBrowserSession {{ SessionId = {SessionId}, Credential = [redacted] }}";
}
