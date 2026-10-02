using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class AuthenticationSecurityRegressionTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory> {
    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RevokeSession_ImmediatelyRejectsItsAccessAndRefreshTokensButKeepsCurrentSession(bool revokeAllOthers) {
        using HttpClient first = factory.CreateClient();
        using HttpClient second = factory.CreateClient();
        string email = $"security-session-{Guid.NewGuid():N}@example.com";
        using HttpResponseMessage registered = await first.PostAsJsonAsync("/api/v1/auth/register", new RegisterHttpRequest(email, "Password123!", "en"));
        registered.EnsureSuccessStatusCode();
        string firstAccess = await ReadAccessTokenAsync(registered);
        string firstRefresh = AuthenticationResponseCookies.ReadRefreshToken(registered);
        first.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", firstAccess);
        using HttpResponseMessage loggedIn = await second.PostAsJsonAsync("/api/v1/auth/login", new LoginHttpRequest(email, "Password123!"));
        loggedIn.EnsureSuccessStatusCode();
        second.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await ReadAccessTokenAsync(loggedIn));
        string sessionId = new JwtSecurityTokenHandler().ReadJwtToken(firstAccess).Claims.Single(claim => string.Equals(claim.Type, JwtSecurityClaimNames.RefreshSessionId, StringComparison.Ordinal)).Value;

        using HttpResponseMessage revoked = await second.DeleteAsync(revokeAllOthers ? "/api/v1/auth/sessions" : $"/api/v1/auth/sessions/{sessionId}");

        Assert.Equal(HttpStatusCode.NoContent, revoked.StatusCode);
        using HttpResponseMessage denied = await first.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        using HttpResponseMessage allowed = await second.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        using HttpResponseMessage refresh = await first.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenHttpRequest(firstRefresh));
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        using HttpResponseMessage currentRefresh = await second.PostAsJsonAsync("/api/v1/auth/refresh", new { });
        Assert.Equal(HttpStatusCode.OK, currentRefresh.StatusCode);
        await ReadAccessTokenAsync(currentRefresh);
    }

    [RequiresDockerFact]
    public async Task Registration_ActiveAndDeletedEmails_HaveSamePublicResponse() {
        using HttpClient client = factory.CreateClient();
        string email = $"security-registration-{Guid.NewGuid():N}@example.com";
        var request = new RegisterHttpRequest(email, "Password123!", "en");
        using HttpResponseMessage registered = await client.PostAsJsonAsync("/api/v1/auth/register", request);
        registered.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await ReadAccessTokenAsync(registered));
        using HttpResponseMessage active = await client.PostAsJsonAsync("/api/v1/auth/register", request);
        using HttpResponseMessage deleted = await client.DeleteAsync("/api/v1/users");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        using HttpResponseMessage duplicateDeleted = await client.PostAsJsonAsync("/api/v1/auth/register", request);
        using var activeJson = JsonDocument.Parse(await active.Content.ReadAsStringAsync());
        using var deletedJson = JsonDocument.Parse(await duplicateDeleted.Content.ReadAsStringAsync());

        Assert.Equal(active.StatusCode, duplicateDeleted.StatusCode);
        foreach (string field in new[] { "error", "message", "errors" }) {
            Assert.Equal(activeJson.RootElement.GetProperty(field).GetRawText(), deletedJson.RootElement.GetProperty(field).GetRawText());
        }
    }

    private static async Task<string> ReadAccessTokenAsync(HttpResponseMessage response) {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(json.RootElement.TryGetProperty("refreshToken", out _));
        return json.RootElement.GetProperty("accessToken").GetString()!;
    }
}
