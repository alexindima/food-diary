using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class BrowserSessionApiTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory> {
    [RequiresDockerFact]
    public async Task CredentialQuotaExhaustion_DoesNotBlockTabRestorationOrLogout() {
        await using WebApplicationFactory<Program> isolated = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.Ordinal) { ["RateLimiting:Auth:PermitLimit"] = "5" })));
        using HttpClient browser = isolated.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (_, string email) = await RegisterIdentityAsync(browser);
        for (int attempt = 0; attempt < 6; attempt++) {
            using HttpResponseMessage login = await browser.PostAsJsonAsync("/api/v1/auth/login", new LoginHttpRequest(email, "WrongPassword123!"));
            Assert.Equal(attempt < 5 ? HttpStatusCode.Unauthorized : HttpStatusCode.TooManyRequests, login.StatusCode);
        }
        for (int tab = 0; tab < 6; tab++) {
            using HttpResponseMessage restored = await browser.PostAsync("/api/v1/auth/session", content: null);
            Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        }
        using HttpResponseMessage logout = await browser.PostAsJsonAsync("/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        using HttpResponseMessage denied = await browser.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
    }

    [RequiresDockerFact]
    public async Task MiniAppSelectedLogout_ProtectsReplacementAndPreservesTheWebsiteSession() {
        using HttpClient browser = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (Guid owner, string email) = await RegisterIdentityAsync(browser);
        browser.DefaultRequestHeaders.Add(BrowserSessionCookieService.ClientHeader, "telegram");
        string oldSession = await MiniAppLoginAsync(browser, email);
        string currentSession = await MiniAppLoginAsync(browser, email);
        browser.DefaultRequestHeaders.Add(BrowserSessionCookieService.SessionHeader, oldSession);

        using HttpResponseMessage staleLogout = await browser.PostAsJsonAsync("/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, staleLogout.StatusCode);
        Assert.False(staleLogout.Headers.Contains("Set-Cookie"));
        browser.DefaultRequestHeaders.Remove(BrowserSessionCookieService.SessionHeader);
        using HttpResponseMessage stillActive = await browser.PostAsJsonAsync("/api/v1/auth/refresh", new { });
        stillActive.EnsureSuccessStatusCode();
        browser.DefaultRequestHeaders.Add(BrowserSessionCookieService.SessionHeader, currentSession);
        using HttpResponseMessage selectedLogout = await browser.PostAsJsonAsync("/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.NoContent, selectedLogout.StatusCode);
        browser.DefaultRequestHeaders.Remove(BrowserSessionCookieService.SessionHeader);
        using HttpResponseMessage rejectedRefresh = await browser.PostAsJsonAsync("/api/v1/auth/refresh", new { });
        // Logout removes the refresh cookie, so an empty refresh request fails input validation.
        Assert.Equal(HttpStatusCode.BadRequest, rejectedRefresh.StatusCode);

        browser.DefaultRequestHeaders.Remove(BrowserSessionCookieService.ClientHeader);
        await AssertIdentityAsync(browser, owner);
    }

    [RequiresDockerFact]
    public async Task MiniAppLogout_WithoutRefreshProof_DoesNotClaimReplacementOrClearWebsiteCookie() {
        using HttpClient browser = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (Guid owner, _) = await RegisterIdentityAsync(browser);
        browser.DefaultRequestHeaders.Add(BrowserSessionCookieService.ClientHeader, "telegram");
        browser.DefaultRequestHeaders.Add(BrowserSessionCookieService.SessionHeader, Guid.NewGuid().ToString("D"));
        using HttpResponseMessage logout = await browser.PostAsJsonAsync("/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, logout.StatusCode);
        JsonElement error = await logout.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Authentication.InvalidToken", error.GetProperty("error").GetString());
        Assert.False(logout.Headers.Contains("Set-Cookie"));
        browser.DefaultRequestHeaders.Remove(BrowserSessionCookieService.ClientHeader);
        browser.DefaultRequestHeaders.Remove(BrowserSessionCookieService.SessionHeader);
        await AssertIdentityAsync(browser, owner);
    }

    private static async Task<string> MiniAppLoginAsync(HttpClient browser, string email) {
        using HttpResponseMessage login = await browser.PostAsJsonAsync("/api/v1/auth/login", new LoginHttpRequest(email, "Password123!"));
        login.EnsureSuccessStatusCode();
        JsonElement response = await login.Content.ReadFromJsonAsync<JsonElement>();
        browser.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", response.GetProperty("accessToken").GetString());
        try {
            using HttpResponseMessage listed = await browser.GetAsync("/api/v1/auth/sessions");
            listed.EnsureSuccessStatusCode();
            JsonElement sessions = await listed.Content.ReadFromJsonAsync<JsonElement>();
            return sessions.EnumerateArray().Single(session => session.GetProperty("isCurrent").GetBoolean())
                .GetProperty("id").GetString()!;
        } finally {
            browser.DefaultRequestHeaders.Authorization = null;
        }
    }

    [RequiresDockerFact]
    public async Task ParallelRestoration_AllTabsKeepTheSameActiveSession() {
        using HttpClient browser = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await RegisterIdentityAsync(browser);
        HttpResponseMessage[] responses = await Task.WhenAll(Enumerable.Range(0, 6)
            .Select(_ => browser.PostAsync("/api/v1/auth/session", content: null)));
        try {
            Assert.True(responses.All(response => response.StatusCode == HttpStatusCode.OK),
                "Parallel browser session restoration must succeed for every tab.");
            Assert.All(responses, response => Assert.False(response.Headers.Contains("Set-Cookie")));
        } finally {
            foreach (HttpResponseMessage response in responses) { response.Dispose(); }
        }
    }

    [RequiresDockerFact]
    public async Task ExplicitMiniAppPasswordLogin_UsesTokensWithoutFallingBackToAnOrdinaryBrowserCookie() {
        using HttpClient browser = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (_, string email) = await RegisterIdentityAsync(browser);
        browser.DefaultRequestHeaders.Add(BrowserSessionCookieService.ClientHeader, "telegram");
        using HttpResponseMessage noToken = await browser.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.Unauthorized, noToken.StatusCode);
        using HttpResponseMessage login = await browser.PostAsJsonAsync("/api/v1/auth/login", new LoginHttpRequest(email, "Password123!"));
        login.EnsureSuccessStatusCode();
        JsonElement payload = await login.Content.ReadFromJsonAsync<JsonElement>();
        string token = payload.GetProperty("accessToken").GetString()!;
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.False(payload.TryGetProperty("refreshToken", out _));
        Assert.False(string.IsNullOrWhiteSpace(AuthenticationResponseCookies.ReadRefreshToken(login)));
        browser.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using HttpResponseMessage profile = await browser.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
    }

    [RequiresDockerFact]
    public async Task AdminSsoAndTwoImpersonationTabs_PreserveTheMainAccountAndIndependentContexts() {
        using HttpClient browser = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using HttpClient firstTarget = factory.CreateClient();
        using HttpClient secondTarget = factory.CreateClient();
        (Guid actorId, string email) = await RegisterIdentityAsync(browser);
        (Guid firstId, _) = await RegisterIdentityAsync(firstTarget);
        (Guid secondId, _) = await RegisterIdentityAsync(secondTarget);
        await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope()) {
            IUserRoleMembershipService roles = scope.ServiceProvider.GetRequiredService<IUserRoleMembershipService>();
            await roles.EnsureRoleAsync(new UserId(actorId), RoleNames.Admin);
        }
        using HttpResponseMessage login = await browser.PostAsJsonAsync("/api/v1/auth/login", new LoginHttpRequest(email, "Password123!"));
        login.EnsureSuccessStatusCode();
        using HttpResponseMessage start = await browser.PostAsync("/api/v1/auth/admin-sso/start", content: null);
        start.EnsureSuccessStatusCode();
        JsonElement handoff = await start.Content.ReadFromJsonAsync<JsonElement>();
        using HttpResponseMessage exchange = await browser.PostAsJsonAsync("/api/v1/auth/admin-sso/exchange", new { Code = handoff.GetProperty("code").GetString() });
        exchange.EnsureSuccessStatusCode();
        Assert.Contains("fooddiary.admin=", AuthenticationResponseCookies.ReadBrowserSession(exchange), StringComparison.Ordinal);
        await AssertIdentityAsync(browser, actorId);

        string first = await ExchangeContextAsync(browser, firstId);
        string second = await ExchangeContextAsync(browser, secondId);
        Assert.NotEqual(first, second, StringComparer.Ordinal);
        browser.DefaultRequestHeaders.Add(BrowserSessionCookieService.ContextHeader, first);
        await AssertIdentityAsync(browser, firstId);
        browser.DefaultRequestHeaders.Remove(BrowserSessionCookieService.ContextHeader);
        browser.DefaultRequestHeaders.Add(BrowserSessionCookieService.ContextHeader, second);
        await AssertIdentityAsync(browser, secondId);
        browser.DefaultRequestHeaders.Remove(BrowserSessionCookieService.ContextHeader);
        await AssertIdentityAsync(browser, actorId);

        browser.DefaultRequestHeaders.Add(BrowserSessionCookieService.ContextHeader, first);
        using HttpResponseMessage logout = await browser.PostAsJsonAsync("/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        using HttpResponseMessage expiredContext = await browser.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.Unauthorized, expiredContext.StatusCode);
        browser.DefaultRequestHeaders.Remove(BrowserSessionCookieService.ContextHeader);
        browser.DefaultRequestHeaders.Add(BrowserSessionCookieService.ContextHeader, second);
        await AssertIdentityAsync(browser, secondId);
        browser.DefaultRequestHeaders.Remove(BrowserSessionCookieService.ContextHeader);
        await AssertIdentityAsync(browser, actorId);

        browser.DefaultRequestHeaders.Add(BrowserSessionCookieService.ClientHeader, "admin");
        using HttpResponseMessage adminLogout = await browser.PostAsJsonAsync("/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.NoContent, adminLogout.StatusCode);
        using HttpResponseMessage rejectedAdmin = await browser.GetAsync("/api/v1/admin/users");
        Assert.Equal(HttpStatusCode.Unauthorized, rejectedAdmin.StatusCode);
        browser.DefaultRequestHeaders.Remove(BrowserSessionCookieService.ClientHeader);
        await AssertIdentityAsync(browser, actorId);
    }

    [RequiresDockerFact]
    public async Task TwoDevices_Coexist_AndLogoutOnlyEndsTheCurrentDevice() {
        using HttpClient computer = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using HttpClient phone = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        string email = $"browser-devices-{Guid.NewGuid():N}@example.com";
        using HttpResponseMessage registered = await computer.PostAsJsonAsync("/api/v1/auth/register", new RegisterHttpRequest(email, "Password123!", "en"));
        registered.EnsureSuccessStatusCode();
        using var registration = JsonDocument.Parse(await registered.Content.ReadAsStringAsync());
        Assert.False(registration.RootElement.TryGetProperty("accessToken", out _));
        Assert.False(registration.RootElement.TryGetProperty("refreshToken", out _));
        Assert.False(string.IsNullOrWhiteSpace(registration.RootElement.GetProperty("sessionId").GetString()));
        string cookie = Assert.Single(registered.Headers.GetValues("Set-Cookie"));
        Assert.Contains(BrowserSessionCookieService.BrowserCookieName + "=", cookie, StringComparison.Ordinal);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);

        using HttpResponseMessage loggedIn = await phone.PostAsJsonAsync("/api/v1/auth/login", new LoginHttpRequest(email, "Password123!", RememberMe: true));
        loggedIn.EnsureSuccessStatusCode();
        using HttpResponseMessage computerInfo = await computer.GetAsync("/api/v1/users/info");
        using HttpResponseMessage phoneInfo = await phone.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.OK, computerInfo.StatusCode);
        Assert.Equal(HttpStatusCode.OK, phoneInfo.StatusCode);
        using HttpResponseMessage logout = await computer.PostAsJsonAsync("/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        using HttpResponseMessage denied = await computer.GetAsync("/api/v1/users/info");
        using HttpResponseMessage allowed = await phone.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [RequiresDockerFact]
    public async Task CookieCannotMintBearerTokens_AndMissingCsrfHeaderCannotLogOut() {
        using HttpClient browser = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using HttpResponseMessage registered = await browser.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterHttpRequest($"browser-csrf-{Guid.NewGuid():N}@example.com", "Password123!", "en"));
        registered.EnsureSuccessStatusCode();
        string credential = AuthenticationResponseCookies.ReadBrowserSession(registered).Split('=')[1];
        using HttpResponseMessage refresh = await browser.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenHttpRequest(credential));
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        browser.DefaultRequestHeaders.Remove("X-Requested-With");
        using HttpResponseMessage rejected = await browser.PostAsJsonAsync("/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.Forbidden, rejected.StatusCode);
        using HttpResponseMessage stillSignedIn = await browser.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.OK, stillSignedIn.StatusCode);
    }

    [RequiresDockerFact]
    public async Task StaleTabCannotLogOutTheReplacementSession() {
        using HttpClient browser = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using HttpResponseMessage registered = await browser.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterHttpRequest($"browser-stale-{Guid.NewGuid():N}@example.com", "Password123!", "en"));
        registered.EnsureSuccessStatusCode();
        browser.DefaultRequestHeaders.Add(BrowserSessionCookieService.SessionHeader, Guid.NewGuid().ToString("D"));
        using HttpResponseMessage rejected = await browser.PostAsJsonAsync("/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
        browser.DefaultRequestHeaders.Remove(BrowserSessionCookieService.SessionHeader);
        using HttpResponseMessage stillSignedIn = await browser.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.OK, stillSignedIn.StatusCode);
    }

    private static async Task<(Guid Id, string Email)> RegisterIdentityAsync(HttpClient client) {
        string email = $"browser-context-{Guid.NewGuid():N}@example.com";
        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterHttpRequest(email, "Password123!", "en"));
        response.EnsureSuccessStatusCode();
        JsonElement payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (payload.GetProperty("user").GetProperty("id").GetGuid(), email);
    }

    private static async Task<string> ExchangeContextAsync(HttpClient browser, Guid target) {
        using HttpResponseMessage start = await browser.PostAsJsonAsync($"/api/v1/admin/users/{target}/impersonation", new { Reason = "Verifying isolated support tabs" });
        start.EnsureSuccessStatusCode();
        JsonElement handoff = await start.Content.ReadFromJsonAsync<JsonElement>();
        using HttpResponseMessage exchange = await browser.PostAsJsonAsync("/api/v1/auth/impersonation/exchange", new { Code = handoff.GetProperty("code").GetString() });
        exchange.EnsureSuccessStatusCode();
        JsonElement payload = await exchange.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(payload.TryGetProperty("accessToken", out _));
        return payload.GetProperty("contextId").GetString()!;
    }

    private static async Task AssertIdentityAsync(HttpClient client, Guid expected) {
        using HttpResponseMessage response = await client.GetAsync("/api/v1/users/info");
        response.EnsureSuccessStatusCode();
        JsonElement payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expected, payload.GetProperty("id").GetGuid());
    }
}
