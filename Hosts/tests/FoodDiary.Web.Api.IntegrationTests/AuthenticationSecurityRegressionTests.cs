using Microsoft.Net.Http.Headers;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class AuthenticationSecurityRegressionTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory> {
    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RepeatLogin_ReplacesBrowserSessionAndKeepsTheOtherBrowser(bool rememberMe) {
        using HttpClient browser = factory.CreateClient();
        using HttpClient otherBrowser = factory.CreateClient();
        string email = $"repeat-session-{Guid.NewGuid():N}@example.com";
        using HttpResponseMessage registered = await browser.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en"));
        registered.EnsureSuccessStatusCode();
        string oldSession = AuthenticationResponseCookies.ReadBrowserSession(registered);
        AuthenticationResponseCookies.ApplyBrowserSession(browser, oldSession);
        using HttpResponseMessage otherLogin = await otherBrowser.PostAsJsonAsync("/api/v1/auth/login",
            new LoginHttpRequest(email, "Password123!"));
        otherLogin.EnsureSuccessStatusCode();
        AuthenticationResponseCookies.ApplyBrowserSession(otherBrowser, AuthenticationResponseCookies.ReadBrowserSession(otherLogin));

        using HttpResponseMessage repeated = await browser.PostAsJsonAsync("/api/v1/auth/login",
            new LoginHttpRequest(email, "Password123!", rememberMe));
        repeated.EnsureSuccessStatusCode();
        AuthenticationResponseCookies.ApplyBrowserSession(browser, AuthenticationResponseCookies.ReadBrowserSession(repeated));
        string setCookie = Assert.Single(repeated.Headers.GetValues("Set-Cookie"), value => value.StartsWith("fooddiary.browser=", StringComparison.Ordinal));
        Assert.Equal(rememberMe, SetCookieHeaderValue.Parse(setCookie).Expires.HasValue);
        using HttpResponseMessage listed = await browser.GetAsync("/api/v1/auth/sessions");
        listed.EnsureSuccessStatusCode();
        using var sessions = JsonDocument.Parse(await listed.Content.ReadAsStringAsync());
        Assert.Equal(2, sessions.RootElement.GetArrayLength());
        Assert.Single(sessions.RootElement.EnumerateArray(), session => session.GetProperty("isCurrent").GetBoolean());

        using HttpClient staleAccessClient = factory.CreateClient();
        AuthenticationResponseCookies.ApplyBrowserSession(staleAccessClient, oldSession);
        using HttpResponseMessage oldAccessDenied = await staleAccessClient.GetAsync("/api/v1/users/info");
        using HttpResponseMessage oldRefreshDenied = await browser.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenHttpRequest(oldSession.Split('=')[1]));
        using HttpResponseMessage otherAllowed = await otherBrowser.GetAsync("/api/v1/users/info");
        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.Unauthorized, oldAccessDenied.StatusCode),
            () => Assert.Equal(HttpStatusCode.Unauthorized, oldRefreshDenied.StatusCode),
            () => Assert.Equal(HttpStatusCode.OK, otherAllowed.StatusCode));

        using HttpResponseMessage failedLogin = await browser.PostAsJsonAsync("/api/v1/auth/login", new LoginHttpRequest(email, "IncorrectPassword123!"));
        Assert.Equal(HttpStatusCode.Unauthorized, failedLogin.StatusCode);
        using HttpResponseMessage stillActive = await browser.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.OK, stillActive.StatusCode);
        using HttpResponseMessage again = await browser.PostAsJsonAsync("/api/v1/auth/login", new LoginHttpRequest(email, "Password123!"));
        again.EnsureSuccessStatusCode();
        AuthenticationResponseCookies.ApplyBrowserSession(browser, AuthenticationResponseCookies.ReadBrowserSession(again));
        using HttpResponseMessage afterAgain = await browser.GetAsync("/api/v1/auth/sessions");
        afterAgain.EnsureSuccessStatusCode();
        using var finalSessions = JsonDocument.Parse(await afterAgain.Content.ReadAsStringAsync());
        Assert.Equal(2, finalSessions.RootElement.GetArrayLength());
    }

    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RevokeSession_ImmediatelyRejectsCookieAndKeepsCurrentSession(bool revokeAllOthers) {
        using HttpClient first = factory.CreateClient();
        using HttpClient second = factory.CreateClient();
        string email = $"security-session-{Guid.NewGuid():N}@example.com";
        using HttpResponseMessage registered = await first.PostAsJsonAsync("/api/v1/auth/register", new RegisterHttpRequest(email, "Password123!", "en"));
        registered.EnsureSuccessStatusCode();
        string firstSession = AuthenticationResponseCookies.ReadBrowserSession(registered);
        AuthenticationResponseCookies.ApplyBrowserSession(first, firstSession);
        using HttpResponseMessage loggedIn = await second.PostAsJsonAsync("/api/v1/auth/login", new LoginHttpRequest(email, "Password123!"));
        loggedIn.EnsureSuccessStatusCode();
        AuthenticationResponseCookies.ApplyBrowserSession(second, AuthenticationResponseCookies.ReadBrowserSession(loggedIn));
        string sessionId = (await registered.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionId").GetString()!;

        using HttpResponseMessage revoked = await second.DeleteAsync(revokeAllOthers ? "/api/v1/auth/sessions" : $"/api/v1/auth/sessions/{sessionId}");

        Assert.Equal(HttpStatusCode.NoContent, revoked.StatusCode);
        using HttpResponseMessage denied = await first.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        using HttpResponseMessage allowed = await second.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        using HttpResponseMessage refresh = await first.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenHttpRequest(firstSession.Split('=')[1]));
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        using HttpResponseMessage currentRefresh = await second.PostAsJsonAsync("/api/v1/auth/session", new { });
        Assert.Equal(HttpStatusCode.OK, currentRefresh.StatusCode);
        Assert.False((await currentRefresh.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("accessToken", out _));
    }

    [RequiresDockerFact]
    public async Task Registration_ActiveAndDeletedEmails_HaveSamePublicResponse() {
        using HttpClient client = factory.CreateClient();
        string email = $"security-registration-{Guid.NewGuid():N}@example.com";
        var request = new RegisterHttpRequest(email, "Password123!", "en");
        using HttpResponseMessage registered = await client.PostAsJsonAsync("/api/v1/auth/register", request);
        registered.EnsureSuccessStatusCode();
        AuthenticationResponseCookies.ApplyBrowserSession(client, AuthenticationResponseCookies.ReadBrowserSession(registered));
        using HttpResponseMessage active = await client.PostAsJsonAsync("/api/v1/auth/register", request);
        using HttpResponseMessage deleted = await client.DeleteAsync("/api/v1/users");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        client.DefaultRequestHeaders.Remove("Cookie");
        using HttpResponseMessage duplicateDeleted = await client.PostAsJsonAsync("/api/v1/auth/register", request);
        using var activeJson = JsonDocument.Parse(await active.Content.ReadAsStringAsync());
        using var deletedJson = JsonDocument.Parse(await duplicateDeleted.Content.ReadAsStringAsync());

        Assert.Equal(active.StatusCode, duplicateDeleted.StatusCode);
        foreach (string field in new[] { "error", "message", "errors" }) {
            Assert.Equal(activeJson.RootElement.GetProperty(field).GetRawText(), deletedJson.RootElement.GetProperty(field).GetRawText());
        }
    }

}
