using System.Net;
using System.Text.Json;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class AdminNotificationTransportTests(TransportApiWebApplicationFactory factory)
    : IClassFixture<TransportApiWebApplicationFactory> {
    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("User", HttpStatusCode.Forbidden)]
    [InlineData("Support", HttpStatusCode.Forbidden)]
    public async Task Schedule_RequiresAdministratorAsync(string? role, HttpStatusCode expected) {
        using HttpClient client = factory.CreateClient();
        if (role is not null) {
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, role);
        }

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/v1/admin/users/{Guid.NewGuid()}/notifications/test", new { type = "FastingCompleted", delaySeconds = 15 });

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_ExposesAdminScheduleContractAsync() {
        using HttpClient client = factory.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        JsonElement operation = document.RootElement["paths"]["/api/v{version}/admin/users/{id}/notifications/test"]["post"];
        foreach (string status in new[] { "202", "400", "401", "403", "404", "429" }) {
            Assert.True(operation.GetProperty("responses").TryGetProperty(status, out _), $"Missing response {status}");
        }

        await PresentationBoundaryIntegrationTests.AssertSnapshotAsync("openapi-full-contract.json",
            PresentationBoundaryIntegrationTests.BuildFullOpenApiSnapshot(document.RootElement));
        await PresentationBoundaryIntegrationTests.AssertSnapshotAsync("openapi-auth-admin-contract.json",
            PresentationBoundaryIntegrationTests.BuildAuthAdminOpenApiSnapshot(document.RootElement));
    }
}
