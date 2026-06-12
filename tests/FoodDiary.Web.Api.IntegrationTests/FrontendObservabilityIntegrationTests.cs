using System.Net;
using System.Net.Http.Json;
using FoodDiary.Presentation.Api.Features.Logs.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class FrontendObservabilityIntegrationTests(ApiWebApplicationFactory apiFactory)
    : IClassFixture<ApiWebApplicationFactory> {
    [Fact]
    public async Task LogsEndpoint_WithValidTelemetryPayload_ReturnsNoContent() {
        HttpClient client = apiFactory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/logs",
            new ClientTelemetryLogHttpRequest(
                Category: "http_request",
                Name: "api.request",
                Level: "info",
                Timestamp: DateTime.UtcNow.ToString("O"),
                Message: "API request completed",
                Route: "/products",
                PageRoute: "/products",
                SessionId: "fd-session-test",
                HttpMethod: "GET",
                Outcome: "success",
                DurationMs: 123.4,
                StatusCode: 200,
                BuildVersion: "test-build"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
