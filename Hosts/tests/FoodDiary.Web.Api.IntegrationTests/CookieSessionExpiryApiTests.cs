using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class CookieSessionExpiryApiTests(CookieExpiryApiWebApplicationFactory factory) : IClassFixture<CookieExpiryApiWebApplicationFactory> {
    [RequiresDockerTheory]
    [InlineData(false, 30)]
    [InlineData(true, 90)]
    public async Task BrowserSession_ExpiresAtItsIdleBoundaryWithoutRestoration(bool rememberMe, int days) {
        factory.Clock.UtcNow = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        DateTimeOffset started = factory.Clock.UtcNow;
        using HttpClient browser = CookieAuthenticationScenario.CreateBrowser(factory);
        CookieIdentity identity = await CookieAuthenticationScenario.RegisterAsync(factory, browser);
        using HttpResponseMessage login = await browser.PostAsJsonAsync("/api/v1/auth/login", new { email = identity.Email, password = CookieAuthenticationScenario.Password, rememberMe });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        DateTimeOffset expiry = await ReadSessionExpiryAsync(browser);
        Assert.Equal(started.AddDays(days), expiry);
        factory.Clock.UtcNow = expiry.AddTicks(-10);
        await CookieAuthenticationScenario.AssertIdentityAsync(browser, identity.Id);
        factory.Clock.UtcNow = expiry;
        await CookieAuthenticationScenario.AssertRejectedSessionAsync(browser);
    }

    [RequiresDockerFact]
    public async Task AdministrativeSession_ExpiresAtItsShortCapWhileTheOrdinarySessionRemainsUsable() {
        factory.Clock.UtcNow = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        using HttpClient browser = CookieAuthenticationScenario.CreateBrowser(factory);
        CookieIdentity administrator = await CookieAuthenticationScenario.CreateAdministratorAsync(factory, browser);
        browser.DefaultRequestHeaders.Add(BrowserSessionCookieService.ClientHeader, "admin");
        DateTimeOffset expiry = await ReadSessionExpiryAsync(browser);
        Assert.InRange(expiry - factory.Clock.UtcNow, TimeSpan.FromSeconds(1), TimeSpan.FromHours(1));
        factory.Clock.UtcNow = expiry.AddTicks(-10);
        using HttpResponseMessage before = await browser.GetAsync("/api/v1/admin/users");
        Assert.Equal(HttpStatusCode.OK, before.StatusCode);
        using HttpResponseMessage restore = await browser.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.OK, restore.StatusCode);
        Assert.Equal(expiry, await ReadSessionExpiryAsync(browser));
        factory.Clock.UtcNow = expiry;
        using HttpResponseMessage denied = await browser.GetAsync("/api/v1/admin/users");
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        await CookieAuthenticationScenario.AssertRejectedSessionAsync(browser);
        browser.DefaultRequestHeaders.Remove(BrowserSessionCookieService.ClientHeader);
        using HttpResponseMessage ordinaryRestore = await browser.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.OK, ordinaryRestore.StatusCode);
        await CookieAuthenticationScenario.AssertIdentityAsync(browser, administrator.Id);
    }

    private async Task<DateTimeOffset> ReadSessionExpiryAsync(HttpClient browser) {
        using HttpResponseMessage response = await browser.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        IRefreshTokenSessionWriteRepository sessions = scope.ServiceProvider.GetRequiredService<IRefreshTokenSessionWriteRepository>();
        UserRefreshTokenSession? session = await sessions.GetByIdAsync(new RefreshTokenSessionId(Guid.Parse(payload.GetProperty("sessionId").GetString()!)), CancellationToken.None);
        Assert.NotNull(session);
        Assert.NotNull(session.ExpiresAtUtc);
        return new DateTimeOffset(session.ExpiresAtUtc.Value);
    }
}
