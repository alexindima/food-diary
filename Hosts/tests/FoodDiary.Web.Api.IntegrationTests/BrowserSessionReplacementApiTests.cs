using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class BrowserSessionReplacementApiTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory> {
    [RequiresDockerTheory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task RepeatLogin_AfterAuthenticationCookieLoss_ReplacesOnlyThisBrowsersSession(bool rememberMe, bool legacyRestore) {
        using HttpClient browser = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        using HttpClient otherDevice = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        string email = $"browser-replacement-{Guid.NewGuid():N}@example.com";
        using HttpResponseMessage registered = await browser.PostAsJsonAsync("/api/v1/auth/register", new RegisterHttpRequest(email, "Password123!", "en"));
        registered.EnsureSuccessStatusCode();
        string originalCookie = AuthenticationResponseCookies.ReadBrowserSession(registered);
        JsonElement original = await registered.Content.ReadFromJsonAsync<JsonElement>();
        string replacementCookie = ReadReplacementCookie(registered);
        if (legacyRestore) {
            // Existing installations have only their authentication cookie before this change.
            browser.DefaultRequestHeaders.Add("Cookie", originalCookie);
            using HttpResponseMessage restored = await browser.PostAsync("/api/v1/auth/session", content: null);
            restored.EnsureSuccessStatusCode();
            replacementCookie = ReadReplacementCookie(restored);
            browser.DefaultRequestHeaders.Remove("Cookie");
        }
        using HttpResponseMessage otherLogin = await otherDevice.PostAsJsonAsync("/api/v1/auth/login", new LoginHttpRequest(email, "Password123!"));
        otherLogin.EnsureSuccessStatusCode();

        // Closing the browser or losing its authentication cookie preserves only the persistent marker.
        if (replacementCookie.Length > 0) {
            browser.DefaultRequestHeaders.Add("Cookie", replacementCookie);
        }
        using HttpResponseMessage markerOnly = await browser.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, markerOnly.StatusCode);
        using HttpResponseMessage login = await browser.PostAsJsonAsync("/api/v1/auth/login", new LoginHttpRequest(email, "Password123!", rememberMe));
        login.EnsureSuccessStatusCode();
        string currentCookie = AuthenticationResponseCookies.ReadBrowserSession(login);
        AuthenticationResponseCookies.ApplyBrowserSession(browser, currentCookie);
        using HttpResponseMessage listed = await browser.GetAsync("/api/v1/auth/sessions");
        listed.EnsureSuccessStatusCode();
        JsonElement sessions = await listed.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(sessions.GetArrayLength() == 2,
            "A repeat sign-in after authentication cookie loss must replace its old session and preserve the other device.");
        Assert.DoesNotContain(sessions.EnumerateArray(), session => string.Equals(
            session.GetProperty("id").GetString(), original.GetProperty("sessionId").GetString(), StringComparison.Ordinal));
        Assert.Single(sessions.EnumerateArray(), session => session.GetProperty("isCurrent").GetBoolean());
        using HttpResponseMessage otherStillActive = await otherDevice.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.OK, otherStillActive.StatusCode);
        AuthenticationResponseCookies.ApplyBrowserSession(browser, originalCookie);
        using HttpResponseMessage oldRejected = await browser.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, oldRejected.StatusCode);
    }

    private static string ReadReplacementCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookies)
            ? cookies.SingleOrDefault(cookie => cookie.StartsWith("fooddiary.browser.previous=", StringComparison.Ordinal))?.Split(';')[0] ?? string.Empty
            : string.Empty;
}
