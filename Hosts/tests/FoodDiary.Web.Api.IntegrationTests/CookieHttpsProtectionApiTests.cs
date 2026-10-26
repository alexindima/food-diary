using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.Net.Http.Headers;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class CookieHttpsProtectionApiTests(CookieHttpsProxyFixture fixture) : IClassFixture<CookieHttpsProxyFixture> {
    [RequiresDockerFact]
    public async Task TlsProxy_ImpersonationCookieRestoresOnlyTheTargetAndLogoutPreservesTheAdministrator() {
        using HttpClient administrator = fixture.CreateBrowser();
        using HttpClient target = fixture.CreateBrowser();
        CookieIdentity actor = await CookieAuthenticationScenario.CreateAdministratorAsync(fixture.Api, administrator);
        CookieIdentity subject = await CookieAuthenticationScenario.RegisterAsync(fixture.Api, target);
        using HttpResponseMessage start = await administrator.PostAsJsonAsync($"/api/v1/admin/users/{subject.Id}/impersonation", new { reason = "Checking HTTPS support context" });
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        JsonElement handoff = await start.Content.ReadFromJsonAsync<JsonElement>();
        using HttpResponseMessage exchange = await administrator.PostAsJsonAsync("/api/v1/auth/impersonation/exchange", new { code = handoff.GetProperty("code").GetString() });
        Assert.Equal(HttpStatusCode.OK, exchange.StatusCode);
        JsonElement payload = await exchange.Content.ReadFromJsonAsync<JsonElement>();
        string context = payload.GetProperty("contextId").GetString()!;
        var cookie = SetCookieHeaderValue.Parse(Assert.Single(exchange.Headers.GetValues("Set-Cookie")));
        Assert.Equal("__Host-fooddiary.impersonation." + context, cookie.Name.ToString());
        Assert.True(cookie.Secure);
        Assert.True(cookie.HttpOnly);
        Assert.Equal(SameSiteMode.Lax, cookie.SameSite);
        Assert.False(cookie.Domain.HasValue);
        administrator.DefaultRequestHeaders.Add(BrowserSessionCookieService.ContextHeader, context);
        await CookieAuthenticationScenario.AssertIdentityAsync(administrator, subject.Id);
        using HttpResponseMessage restored = await administrator.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        using HttpResponseMessage logout = await administrator.PostAsJsonAsync("/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        await CookieAuthenticationScenario.AssertRejectedSessionAsync(administrator);
        administrator.DefaultRequestHeaders.Remove(BrowserSessionCookieService.ContextHeader);
        await CookieAuthenticationScenario.AssertIdentityAsync(administrator, actor.Id);
        using HttpResponseMessage admin = await administrator.GetAsync("/api/v1/admin/users");
        Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
    }

    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TlsProxy_IssuesHostOnlySecureHttpOnlyCookiesAndRestoresThemInANewHttpClient(bool rememberMe) {
        var jar = new CookieContainer();
        using HttpClient browser = fixture.CreateBrowser(jar);
        CookieIdentity identity = await CookieAuthenticationScenario.RegisterAsync(fixture.Api, browser);
        using HttpResponseMessage login = await browser.PostAsJsonAsync("/api/v1/auth/login",
            new LoginHttpRequest(identity.Email, CookieAuthenticationScenario.Password, rememberMe));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var cookie = SetCookieHeaderValue.Parse(Assert.Single(login.Headers.GetValues("Set-Cookie")));
        Assert.Equal("__Host-" + BrowserSessionCookieService.BrowserCookieName, cookie.Name.ToString());
        Assert.True(cookie.Secure);
        Assert.True(cookie.HttpOnly);
        Assert.Equal(SameSiteMode.Lax, cookie.SameSite);
        Assert.Equal("/", cookie.Path.ToString());
        Assert.False(cookie.Domain.HasValue);
        Assert.Equal(rememberMe, cookie.Expires.HasValue);
        Assert.True(login.Headers.CacheControl!.NoStore);
        Assert.True(login.Headers.CacheControl.NoCache);
        JsonElement payload = await login.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(payload.TryGetProperty("accessToken", out _));
        Assert.False(payload.TryGetProperty("refreshToken", out _));
        Assert.DoesNotContain(jar.GetCookies(new Uri("http://localhost")).Cast<Cookie>(), value => value.Name.StartsWith("__Host-", StringComparison.Ordinal));

        using HttpClient reloaded = fixture.CreateBrowser(jar);
        using HttpResponseMessage restored = await reloaded.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        Assert.False(restored.Headers.Contains("Set-Cookie"));
        await CookieAuthenticationScenario.AssertIdentityAsync(reloaded, identity.Id);
    }

    [RequiresDockerTheory]
    [InlineData("missing-header")]
    [InlineData("foreign-origin")]
    public async Task TlsProxy_RejectsUntrustedLogoutAndPreservesTheActualSession(string attack) {
        using HttpClient browser = fixture.CreateBrowser();
        CookieIdentity identity = await CookieAuthenticationScenario.RegisterAsync(fixture.Api, browser);
        if (string.Equals(attack, "missing-header", StringComparison.Ordinal)) {
            browser.DefaultRequestHeaders.Remove("X-Requested-With");
        } else {
            browser.DefaultRequestHeaders.Add("Origin", "https://evil.example");
            // The proxy must overwrite these client-supplied values before the API evaluates the origin.
            browser.DefaultRequestHeaders.Add("X-Forwarded-Host", "evil.example");
            browser.DefaultRequestHeaders.Add("X-Forwarded-Proto", "http");
        }
        using HttpResponseMessage rejected = await browser.PostAsJsonAsync("/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.Forbidden, rejected.StatusCode);
        Assert.False(rejected.Headers.Contains("Set-Cookie"));
        browser.DefaultRequestHeaders.Remove("Origin");
        browser.DefaultRequestHeaders.Remove("X-Forwarded-Host");
        browser.DefaultRequestHeaders.Remove("X-Forwarded-Proto");
        await CookieAuthenticationScenario.AssertIdentityAsync(browser, identity.Id);
        if (string.Equals(attack, "missing-header", StringComparison.Ordinal)) { browser.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest"); }
        browser.DefaultRequestHeaders.Add("Origin", fixture.Origin.GetLeftPart(UriPartial.Authority));
        using HttpResponseMessage logout = await browser.PostAsJsonAsync("/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        await CookieAuthenticationScenario.AssertRejectedSessionAsync(browser);
    }

    [RequiresDockerFact]
    public async Task TlsProxy_AdminCookieRequiresSameOriginEvenWhenAnotherOriginIsAllowedByCors() {
        using HttpClient administrator = fixture.CreateBrowser();
        CookieIdentity identity = await CookieAuthenticationScenario.CreateAdministratorAsync(fixture.Api, administrator);
        using HttpResponseMessage before = await administrator.GetAsync("/api/v1/admin/users");
        Assert.Equal(HttpStatusCode.OK, before.StatusCode);
        administrator.DefaultRequestHeaders.Add(BrowserSessionCookieService.ClientHeader, "admin");
        administrator.DefaultRequestHeaders.Add("Origin", "https://trusted-other.example");
        using HttpResponseMessage denied = await administrator.PostAsJsonAsync("/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        administrator.DefaultRequestHeaders.Remove("Origin");
        using HttpResponseMessage stillActive = await administrator.GetAsync("/api/v1/admin/users");
        Assert.Equal(HttpStatusCode.OK, stillActive.StatusCode);
        administrator.DefaultRequestHeaders.Add("Origin", fixture.Origin.GetLeftPart(UriPartial.Authority));
        using HttpResponseMessage logout = await administrator.PostAsJsonAsync("/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        using HttpResponseMessage rejected = await administrator.GetAsync("/api/v1/admin/users");
        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
        administrator.DefaultRequestHeaders.Remove(BrowserSessionCookieService.ClientHeader);
        await CookieAuthenticationScenario.AssertIdentityAsync(administrator, identity.Id);
    }
}
