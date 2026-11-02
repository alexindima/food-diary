using System.Text.Json;
using FoodDiary.Telegram.Bot.Api.Generated.Model;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class GeneratedAuthenticationContractTests {
    [Fact]
    public void AuthenticationResponse_RoundTripsSessionRolesAndImpersonationMetadata() {
        var original = new AuthenticationHttpResponse {
            AccessToken = "synthetic-access-token",
            SessionId = "synthetic-session",
            Roles = ["User", "Premium"],
            RememberMe = true,
            ImpersonationContext = "synthetic-context",
            ImpersonationReason = "Support request",
            User = new UserHttpResponse(),
        };

        string json = JsonSerializer.Serialize(original);
        AuthenticationHttpResponse response = Assert.IsType<AuthenticationHttpResponse>(JsonSerializer.Deserialize<AuthenticationHttpResponse>(json));

        Assert.Multiple(
            () => Assert.Equal(original.AccessToken, response.AccessToken),
            () => Assert.Equal(original.SessionId, response.SessionId),
            () => Assert.Equal(original.Roles, response.Roles),
            () => Assert.True(response.RememberMe),
            () => Assert.Equal(original.ImpersonationContext, response.ImpersonationContext),
            () => Assert.Equal(original.ImpersonationReason, response.ImpersonationReason),
            () => Assert.Contains("\"sessionId\":", json, StringComparison.Ordinal),
            () => Assert.Contains("\"rememberMe\":true", json, StringComparison.Ordinal));
    }
}
