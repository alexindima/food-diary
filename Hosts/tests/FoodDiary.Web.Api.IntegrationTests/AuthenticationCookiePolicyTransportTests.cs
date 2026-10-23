using System.Net;
using System.Text.Json;
using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class AuthenticationCookiePolicyTransportTests(TransportApiWebApplicationFactory factory)
    : IClassFixture<TransportApiWebApplicationFactory> {
    [Theory]
    [InlineData("main", true)]
    [InlineData("independent-bearer", false)]
    public async Task ActualMvcPipeline_AppliesActionCookiePolicyWithoutChangingAuthenticationBody(string action, bool writesCookie) {
        using HttpClient client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/test-auth-cookie/{action}");
        request.Headers.Add("Cookie", $"{RefreshTokenCookieService.CookieName}=existing-main-session");

        using HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("test-access", body.RootElement.GetProperty("accessToken").GetString());
        Assert.False(body.RootElement.TryGetProperty("refreshToken", out _));
        string[] cookies = response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? values) ? [.. values] : [];
        if (writesCookie) {
            string cookie = Assert.Single(cookies);
            Assert.Contains($"{RefreshTokenCookieService.CookieName}=test-refresh", cookie, StringComparison.Ordinal);
            Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("path=/api/v1/auth", cookie, StringComparison.OrdinalIgnoreCase);
        } else {
            Assert.Empty(cookies);
        }
    }
}
