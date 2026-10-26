using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class CookieImpersonationBoundaryApiTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory> {
    private const string Reason = "Investigating the selected support context";

    [RequiresDockerFact]
    public async Task Restore_SelectedContextReturnsOnlyTheTargetIdentityRolesAndReason() {
        ContextProof proof = await CreateContextAsync();
        using HttpClient browser = CreateSelectedBrowser(proof);
        using HttpResponseMessage response = await browser.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(proof.Target.Id, payload.GetProperty("user").GetProperty("id").GetGuid());
        Assert.Equal(proof.Id, payload.GetProperty("impersonationContext").GetString());
        Assert.Equal(Reason, payload.GetProperty("impersonationReason").GetString());
        Assert.DoesNotContain(payload.GetProperty("roles").EnumerateArray(), role => string.Equals(role.GetString(), "Admin", StringComparison.Ordinal));
        Assert.False(payload.TryGetProperty("accessToken", out _));
        Assert.False(payload.TryGetProperty("refreshToken", out _));
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [RequiresDockerTheory]
    [InlineData("missing-cookie")]
    [InlineData("short-secret")]
    [InlineData("wrong-secret")]
    [InlineData("invalid-selector")]
    public async Task InvalidContext_CannotFallBackToAValidOrdinaryCookie(string fault) {
        ContextProof proof = await CreateContextAsync();
        string id = proof.Id;
        string cookie = proof.Cookie;
        string[] parts = cookie.Split('=', 2);
        switch (fault) {
            case "missing-cookie": cookie = string.Empty; break;
            case "short-secret": cookie = parts[0] + "=short"; break;
            case "wrong-secret": cookie = parts[0] + "=" + new string('x', 43); break;
            case "invalid-selector": id = new string('z', 32); break;
        }
        using HttpClient browser = CreateSelectedBrowser(proof with { Id = id, Cookie = cookie });
        await CookieAuthenticationScenario.AssertRejectedSessionAsync(browser);
        // Anonymous context logout must never turn into logout of the valid ordinary account.
        using HttpResponseMessage logout = await browser.PostAsJsonAsync("/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        browser.DefaultRequestHeaders.Remove(BrowserSessionCookieService.ContextHeader);
        await CookieAuthenticationScenario.AssertIdentityAsync(browser, proof.Target.Id);
    }

    [RequiresDockerTheory]
    [InlineData("GET", "/api/v1/users/info")]
    [InlineData("POST", "/api/v1/auth/session")]
    public async Task PresentEmptyContextSelector_IsRejectedAtTheHttpServerBoundary(string method, string path) {
        ContextProof proof = await CreateContextAsync();
        // HttpClient/TestServer can omit a zero-length header during copying. Supply the present header
        // at the server boundary so the test distinguishes an empty selector from an absent selector.
        HttpContext response = await factory.Server.SendAsync(context => {
            context.Request.Method = method;
            context.Request.Path = path;
            context.Request.Headers.Cookie = proof.Target.Cookie;
            context.Request.Headers[BrowserSessionCookieService.ContextHeader] = string.Empty;
            context.Request.Headers.XRequestedWith = "XMLHttpRequest";
            Assert.True(context.Request.Headers.ContainsKey(BrowserSessionCookieService.ContextHeader));
        });
        Assert.Equal(401, response.Response.StatusCode);
    }

    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingOrUndecryptableCacheEntry_RejectsContextWithoutUsingTheOrdinaryCookie(bool corrupt) {
        ContextProof proof = await CreateContextAsync();
        using HttpClient browser = CreateSelectedBrowser(proof);
        await CookieAuthenticationScenario.AssertIdentityAsync(browser, proof.Target.Id);
        IDistributedCache cache = factory.Services.GetRequiredService<IDistributedCache>();
        // Address the owned entry only to inject an infrastructure fault; authorization is exercised over HTTP.
        string key = "fooddiary.impersonation." + proof.Id + "." + SecurityTokenGenerator.HashForStorage(proof.Cookie.Split('=', 2)[1]);
        if (corrupt) {
            await cache.SetStringAsync(key, "not-a-data-protection-payload");
        } else {
            await cache.RemoveAsync(key);
        }
        await CookieAuthenticationScenario.AssertRejectedSessionAsync(browser);
        browser.DefaultRequestHeaders.Remove(BrowserSessionCookieService.ContextHeader);
        await CookieAuthenticationScenario.AssertIdentityAsync(browser, proof.Target.Id);
    }

    [RequiresDockerTheory]
    [InlineData("/api/v1/users/password", "PATCH")]
    [InlineData("/api/v1/auth/sessions", "DELETE")]
    [InlineData("/api/v1/auth/admin-sso/start", "POST")]
    public async Task SelectedContext_CannotChangeCredentialsRevokeSessionsOrStartAdministrativeSso(string path, string method) {
        ContextProof proof = await CreateContextAsync();
        using HttpClient browser = CreateSelectedBrowser(proof);
        using var request = new HttpRequestMessage(new HttpMethod(method), path) {
            Content = JsonContent.Create(new { currentPassword = CookieAuthenticationScenario.Password, newPassword = "UnwantedPassword456!" }),
        };
        using HttpResponseMessage denied = await browser.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        await CookieAuthenticationScenario.AssertIdentityAsync(browser, proof.Target.Id);
        browser.DefaultRequestHeaders.Remove(BrowserSessionCookieService.ContextHeader);
        await CookieAuthenticationScenario.AssertIdentityAsync(browser, proof.Target.Id);
        using HttpClient independent = CookieAuthenticationScenario.CreateBrowser(factory);
        await CookieAuthenticationScenario.LoginAsync(independent, proof.Target.Email);
        await CookieAuthenticationScenario.AssertIdentityAsync(independent, proof.Target.Id);
    }

    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HubNegotiation_AppliesReadOnlyPolicyToTheSelectedContextAndRejectsInvalidSelection(bool querySelector) {
        ContextProof proof = await CreateContextAsync();
        using HttpClient browser = CreateSelectedBrowser(proof);
        string path = "/hubs/notifications/negotiate?negotiateVersion=1";
        if (querySelector) {
            browser.DefaultRequestHeaders.Remove(BrowserSessionCookieService.ContextHeader);
            path += "&fd_context=" + proof.Id;
        }
        using HttpResponseMessage accepted = await browser.PostAsync(path, content: null);
        Assert.Equal(HttpStatusCode.Forbidden, accepted.StatusCode);
        using HttpClient ordinary = factory.CreateClient();
        AuthenticationResponseCookies.ApplyBrowserSession(ordinary, proof.Target.Cookie);
        using HttpResponseMessage allowed = await ordinary.PostAsync("/hubs/notifications/negotiate?negotiateVersion=1", content: null);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        if (querySelector) {
            path = "/hubs/notifications/negotiate?negotiateVersion=1&fd_context=invalid";
        } else {
            browser.DefaultRequestHeaders.Remove(BrowserSessionCookieService.ContextHeader);
            browser.DefaultRequestHeaders.Add(BrowserSessionCookieService.ContextHeader, "invalid");
        }
        using HttpResponseMessage rejected = await browser.PostAsync(path, content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
    }

    private HttpClient CreateSelectedBrowser(ContextProof proof) {
        HttpClient browser = factory.CreateClient();
        AuthenticationResponseCookies.ApplyBrowserSession(browser, proof.Target.Cookie + "; " + proof.Cookie);
        browser.DefaultRequestHeaders.Add(BrowserSessionCookieService.ContextHeader, proof.Id);
        return browser;
    }

    private async Task<ContextProof> CreateContextAsync() {
        using HttpClient administrator = CookieAuthenticationScenario.CreateBrowser(factory);
        using HttpClient target = CookieAuthenticationScenario.CreateBrowser(factory);
        await CookieAuthenticationScenario.CreateAdministratorAsync(factory, administrator);
        CookieIdentity identity = await CookieAuthenticationScenario.RegisterAsync(factory, target);
        using HttpResponseMessage start = await administrator.PostAsJsonAsync($"/api/v1/admin/users/{identity.Id}/impersonation", new { reason = Reason });
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        JsonElement handoff = await start.Content.ReadFromJsonAsync<JsonElement>();
        using HttpResponseMessage exchange = await administrator.PostAsJsonAsync("/api/v1/auth/impersonation/exchange", new { code = handoff.GetProperty("code").GetString() });
        Assert.Equal(HttpStatusCode.OK, exchange.StatusCode);
        JsonElement payload = await exchange.Content.ReadFromJsonAsync<JsonElement>();
        return new ContextProof(payload.GetProperty("contextId").GetString()!, Assert.Single(exchange.Headers.GetValues("Set-Cookie")).Split(';')[0], identity);
    }

    [ExcludeFromCodeCoverage]
    private sealed record ContextProof(string Id, string Cookie, CookieIdentity Target);
}
