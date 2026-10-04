using System.Text.Json;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class StatisticsCalendarTransportIntegrationTests(TransportApiWebApplicationFactory factory)
    : IClassFixture<TransportApiWebApplicationFactory> {
    [Fact]
    public async Task SwaggerStatistics_ExposesOptionalCalendarZone_AndMatchesContractSnapshots() {
        using HttpClient client = factory.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        JsonElement paths = document.RootElement.GetProperty("paths");
        foreach (string path in new[] { "/api/v{version}/statistics", "/api/v{version}/statistics/summary" }) {
            JsonElement zone = Assert.Single(paths.GetProperty(path).GetProperty("get").GetProperty("parameters")
                .EnumerateArray(), static parameter => string.Equals(parameter.GetProperty("name").GetString(), "TimeZoneId", StringComparison.Ordinal));
            Assert.Equal("query", zone.GetProperty("in").GetString());
            Assert.False(zone.TryGetProperty("required", out JsonElement required) && required.GetBoolean());
            Assert.Equal("string", zone.GetProperty("schema").GetProperty("type").GetString());
            Assert.Equal(100, zone.GetProperty("schema").GetProperty("maxLength").GetInt32());
        }

        await PresentationBoundaryIntegrationTests.AssertSnapshotAsync("openapi-full-contract.json",
            PresentationBoundaryIntegrationTests.BuildFullOpenApiSnapshot(document.RootElement));
        await PresentationBoundaryIntegrationTests.AssertSnapshotAsync("openapi-focused-contract.json",
            PresentationBoundaryIntegrationTests.BuildFocusedOpenApiSnapshot(document.RootElement));
    }
}
