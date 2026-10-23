namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;

public sealed record AuthenticationClientContext(
    string AuthProvider,
    string? IpAddress,
    string? UserAgent,
    [property: System.Text.Json.Serialization.JsonIgnore] string? ExistingRefreshToken = null) {
    public override string ToString() => $"AuthenticationClientContext {{ AuthProvider = {AuthProvider}, ExistingRefreshToken = [redacted] }}";
}
