using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class ClientTelemetryWebVitalsContractTests(TransportApiWebApplicationFactory factory)
    : IClassFixture<TransportApiWebApplicationFactory> {
    [Fact]
    public async Task AnonymousWebVitals_MatchThePublishedTransportContractAsync() {
        using var contract = JsonDocument.Parse(await File.ReadAllTextAsync(
            SnapshotPathResolver.GetPath("client-telemetry-web-vitals-contract.json")));
        string route = contract.RootElement.GetProperty("route").GetString()!;
        using HttpClient client = factory.CreateClient();
        foreach (JsonElement sample in contract.RootElement.GetProperty("cases").EnumerateArray()) {
            var payload = new {
                category = "web_vital",
                name = sample.GetProperty("name").GetString(),
                level = "info",
                timestamp = DateTimeOffset.UtcNow.ToString("O"),
                value = sample.GetProperty("value").GetDouble(),
                unit = sample.GetProperty("unit").GetString(),
                pageRoute = "/dashboard",
                details = new { metricId = "v6-1234567890123-1234567890123" },
            };

            using HttpResponseMessage response = await client.PostAsJsonAsync(route, payload);

            Assert.Equal(sample.GetProperty("expectedStatus").GetInt32(), (int)response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        }
    }
}
