using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Marketing.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class MarketingAttributionIntegrationTests(ApiWebApplicationFactory apiFactory)
    : IClassFixture<ApiWebApplicationFactory> {
    [Fact]
    public async Task AttributionEndpoint_WithValidPayload_ReturnsNoContent() {
        HttpClient client = apiFactory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post,
            "/api/v1/marketing/attribution-events") {
            Content = JsonContent.Create(new MarketingAttributionHttpRequest(
                Timestamp: DateTime.UtcNow.ToString("O"),
                AnonymousId: "fd-anon-test",
                SessionId: "fd-session-test",
                LandingPath: "/?utm_source=telegram&utm_medium=social&utm_campaign=launch",
                ReferrerHost: "t.me",
                UtmSource: "telegram",
                UtmMedium: "social",
                UtmCampaign: "launch",
                BuildVersion: "test-build")),
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));
        HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task AttributionEndpoint_WithClientSelectedSignupAndUserId_ReturnsBadRequest() {
        HttpClient client = apiFactory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post,
            "/api/v1/marketing/attribution-events") {
            Content = JsonContent.Create(new {
                eventType = "signup_completed",
                timestamp = DateTime.UtcNow.ToString("O"),
                userId = Guid.NewGuid(),
                anonymousId = "fd-anon-test",
                sessionId = "fd-session-test",
                landingPath = "/",
            }),
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));
        HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AttributionEndpoint_WithoutEventId_ReturnsBadRequest() {
        HttpClient client = apiFactory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/marketing/attribution-events",
            new MarketingAttributionHttpRequest(
                Timestamp: DateTime.UtcNow.ToString("O"),
                AnonymousId: "fd-anon-test",
                SessionId: "fd-session-test",
                LandingPath: "/"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SignupAttributionEndpoint_WithoutAuthentication_ReturnsUnauthorized() {
        HttpClient client = apiFactory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post,
            "/api/v1/marketing/attribution-events/signup") {
            Content = JsonContent.Create(new MarketingSignupAttributionHttpRequest(
                Timestamp: DateTime.UtcNow.ToString("O"),
                AnonymousId: "fd-anon-test",
                SessionId: "fd-session-test",
                LandingPath: "/")),
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));
        HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SignupAttributionEndpoint_WithAuthentication_ReturnsNoContent() {
        HttpClient client = apiFactory.CreateClient();
        HttpResponseMessage registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest($"marketing-attribution-{Guid.NewGuid():N}@example.com", "Password123!", "en"));
        AuthPayload? auth = await registerResponse.Content.ReadFromJsonAsync<AuthPayload>();
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        Assert.NotNull(auth);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        using var request = new HttpRequestMessage(HttpMethod.Post,
            "/api/v1/marketing/attribution-events/signup") {
            Content = JsonContent.Create(new MarketingSignupAttributionHttpRequest(
                Timestamp: DateTime.UtcNow.ToString("O"),
                AnonymousId: "fd-anon-test",
                SessionId: "fd-session-test",
                LandingPath: "/")),
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));
        HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [ExcludeFromCodeCoverage]
    private sealed record AuthPayload(string AccessToken);
}
