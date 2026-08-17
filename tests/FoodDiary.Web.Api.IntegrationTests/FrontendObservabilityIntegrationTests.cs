using System.Net;
using System.Net.Http.Json;
using System.Text;
using FoodDiary.Presentation.Api.Features.Logs;
using FoodDiary.Presentation.Api.Features.Logs.Requests;
using FoodDiary.Presentation.Api.Responses;
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

    [Fact]
    public async Task LogsEndpoint_WithUnknownEventName_ReturnsBadRequest() {
        HttpClient client = apiFactory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/logs",
            new ClientTelemetryLogHttpRequest(
                Category: "user_action",
                Name: "fasting.attacker-controlled",
                Level: "info",
                Timestamp: DateTime.UtcNow.ToString("O")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LogsEndpoint_WithPayloadAboveLimit_ReturnsPayloadTooLarge() {
        HttpClient client = apiFactory.CreateClient();
        string payload = $$"""
            {
              "category": "client_error",
              "name": "global-error",
              "level": "error",
              "timestamp": "{{DateTime.UtcNow:O}}",
              "stack": "{{new string('x', LogsController.MaxPayloadBytes)}}"
            }
            """;

        HttpResponseMessage response = await client.PostAsync(
            "/api/v1/logs",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        ApiErrorHttpResponse? error = await response.Content.ReadFromJsonAsync<ApiErrorHttpResponse>();
        Assert.NotNull(error);
        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode),
            () => Assert.Equal("Request.PayloadTooLarge", error.Error),
            () => Assert.Equal("The request payload is too large.", error.Message),
            () => Assert.False(string.IsNullOrWhiteSpace(error.TraceId)));
    }
}
