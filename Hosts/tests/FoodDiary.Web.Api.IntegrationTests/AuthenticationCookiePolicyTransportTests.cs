using System.Net;
using System.Text.Json;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class AuthenticationCookiePolicyTransportTests(TransportApiWebApplicationFactory factory)
    : IClassFixture<TransportApiWebApplicationFactory> {
    [Theory]
    [InlineData("login")]
    [InlineData("session")]
    [InlineData("logout")]
    [InlineData("impersonation/exchange")]
    [InlineData("LOGIN")]
    [InlineData("SESSION")]
    public async Task BrowserAuthenticationEntry_RejectsMissingCsrfHeaderBeforeHandlers(string action) {
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Remove("X-Requested-With");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/{action}");

        using HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Authentication.InvalidBrowserRequest", body.RootElement.GetProperty("error").GetString());
        Assert.False(body.RootElement.TryGetProperty("accessToken", out _));
        Assert.False(body.RootElement.TryGetProperty("refreshToken", out _));
        string[] cookies = response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? values) ? [.. values] : [];
        Assert.Empty(cookies);
    }
}
