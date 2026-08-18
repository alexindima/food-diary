using System.Net;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class CorsIntegrationTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory> {
    [Fact]
    public async Task Preflight_FromConfiguredDevelopmentOrigin_ReturnsCorsHeaders() {
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage request = CreatePreflightRequest("http://localhost:4200");

        using HttpResponseMessage response = await client.SendAsync(request);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.NoContent, response.StatusCode),
            () => Assert.Equal(
                "http://localhost:4200",
                Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin"))),
            () => Assert.Equal(
                "true",
                Assert.Single(response.Headers.GetValues("Access-Control-Allow-Credentials"))));
    }

    [Fact]
    public async Task Preflight_FromUnconfiguredOrigin_DoesNotReturnCorsHeaders() {
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage request = CreatePreflightRequest("https://untrusted.example");

        using HttpResponseMessage response = await client.SendAsync(request);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.NoContent, response.StatusCode),
            () => Assert.False(response.Headers.Contains("Access-Control-Allow-Origin")),
            () => Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials")));
    }

    private static HttpRequestMessage CreatePreflightRequest(string origin) {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/version");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        return request;
    }
}
