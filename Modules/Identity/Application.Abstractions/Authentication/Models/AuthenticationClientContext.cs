using System.Text.Json.Serialization;

namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;

public sealed record AuthenticationClientContext(
    string AuthProvider,
    string? IpAddress,
    string? UserAgent,
    [property: JsonIgnore] string? ExistingRefreshToken = null,
    AuthenticationCredentialKind CredentialKind = AuthenticationCredentialKind.Bearer,
    [property: JsonIgnore] string? ExistingBrowserCredential = null) {
    public override string ToString() => $"AuthenticationClientContext {{ AuthProvider = {AuthProvider}, ExistingRefreshToken = [redacted] }}";
}
