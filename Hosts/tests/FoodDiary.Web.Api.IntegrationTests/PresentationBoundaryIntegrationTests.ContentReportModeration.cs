using System.Net;
using System.Net.Http.Json;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

public sealed partial class PresentationBoundaryIntegrationTests {
    [RequiresDockerTheory]
    [InlineData("review")]
    [InlineData("dismiss")]
    public async Task ContentReportModeration_WithOversizedNote_ReturnsValidationContract(string action) {
        using HttpClient client = testAuthFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, PresentationRoleNames.Admin);
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/v1/admin/moderation/{Guid.NewGuid()}/{action}",
            new { AdminNote = new string('x', 2001) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ErrorPayload? payload = await response.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);
        Assert.NotNull(payload);
        await AssertErrorContractSnapshotAsync("admin-report-note-too-long", payload);
    }

    [RequiresDockerTheory]
    [InlineData("review", false)]
    [InlineData("dismiss", false)]
    [InlineData("review", true)]
    [InlineData("dismiss", true)]
    public async Task ContentReportModeration_WithBoundaryNote_ReachesReportLookup(string action, bool padding) {
        using HttpClient client = testAuthFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, PresentationRoleNames.Admin);
        string note = new('x', 2000);
        if (padding) {
            note = "  " + note + "  ";
        }
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/v1/admin/moderation/{Guid.NewGuid()}/{action}", new { AdminNote = note });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        ErrorPayload? payload = await response.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal("ContentReport.NotFound", payload.Error);
    }
}
