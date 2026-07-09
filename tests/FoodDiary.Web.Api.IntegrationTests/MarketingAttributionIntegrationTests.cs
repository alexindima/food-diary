using System.Net;
using System.Net.Http.Json;
using FoodDiary.Presentation.Api.Features.Marketing.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class MarketingAttributionIntegrationTests(ApiWebApplicationFactory apiFactory)
    : IClassFixture<ApiWebApplicationFactory> {
    [Fact]
    public async Task AttributionEndpoint_WithValidPayload_ReturnsNoContent() {
        HttpClient client = apiFactory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/marketing/attribution-events",
            new MarketingAttributionHttpRequest(
                EventType: "page_landing",
                Timestamp: DateTime.UtcNow.ToString("O"),
                UserId: null,
                AnonymousId: "fd-anon-test",
                SessionId: "fd-session-test",
                LandingPath: "/?utm_source=telegram&utm_medium=social&utm_campaign=launch",
                ReferrerHost: "t.me",
                UtmSource: "telegram",
                UtmMedium: "social",
                UtmCampaign: "launch",
                BuildVersion: "test-build"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
