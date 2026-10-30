using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class BrowserSessionPersistenceApiTests(CookieExpiryApiWebApplicationFactory factory) : IClassFixture<CookieExpiryApiWebApplicationFactory> {
    [RequiresDockerFact]
    public async Task RememberRequest_DoesNotPromoteOrExtendAnAdministrativeSession() {
        factory.Clock.UtcNow = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        using HttpClient browser = CookieAuthenticationScenario.CreateBrowser(factory);
        await CookieAuthenticationScenario.CreateAdministratorAsync(factory, browser);
        browser.DefaultRequestHeaders.Add(BrowserSessionCookieService.ClientHeader, "admin");
        using HttpResponseMessage initial = await browser.PostAsync("/api/v1/auth/session", content: null);
        initial.EnsureSuccessStatusCode();
        JsonElement original = await initial.Content.ReadFromJsonAsync<JsonElement>();
        var id = new RefreshTokenSessionId(Guid.Parse(original.GetProperty("sessionId").GetString()!));
        UserRefreshTokenSession before = await ReadSessionAsync(id);
        using HttpResponseMessage restored = await browser.PostAsync("/api/v1/auth/session?rememberMe=true", content: null);
        restored.EnsureSuccessStatusCode();
        Assert.False((await restored.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("rememberMe").GetBoolean());
        UserRefreshTokenSession after = await ReadSessionAsync(id);
        Assert.Multiple(() => {
            Assert.False(after.RememberMe);
            Assert.Equal(before.ExpiresAtUtc, after.ExpiresAtUtc);
            Assert.False(restored.Headers.Contains("Set-Cookie"));
        });
    }

    [RequiresDockerFact]
    public async Task RememberRequest_CannotReviveAnExpiredSession() {
        factory.Clock.UtcNow = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        using HttpClient browser = CookieAuthenticationScenario.CreateBrowser(factory);
        string email = $"pwa-expired-{Guid.NewGuid():N}@example.com";
        using HttpResponseMessage registered = await browser.PostAsJsonAsync("/api/v1/auth/register", new RegisterHttpRequest(email, "Password123!", "en"));
        registered.EnsureSuccessStatusCode();
        JsonElement original = await registered.Content.ReadFromJsonAsync<JsonElement>();
        var id = new RefreshTokenSessionId(Guid.Parse(original.GetProperty("sessionId").GetString()!));
        UserRefreshTokenSession session = await ReadSessionAsync(id);
        factory.Clock.UtcNow = new DateTimeOffset(session.ExpiresAtUtc!.Value);
        using HttpResponseMessage restored = await browser.PostAsync("/api/v1/auth/session?rememberMe=true", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, restored.StatusCode);
        Assert.False(restored.Headers.Contains("Set-Cookie"));
        Assert.False((await ReadSessionAsync(id)).RememberMe);
    }

    [RequiresDockerFact]
    public async Task InstalledPwa_PromotesExistingSession_AndRestoresAfterBrowserRestart() {
        factory.Clock.UtcNow = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        using HttpClient browser = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        string email = $"pwa-persistence-{Guid.NewGuid():N}@example.com";
        using HttpResponseMessage registered = await browser.PostAsJsonAsync("/api/v1/auth/register", new RegisterHttpRequest(email, "Password123!", "en"));
        registered.EnsureSuccessStatusCode();
        JsonElement original = await registered.Content.ReadFromJsonAsync<JsonElement>();
        var id = new RefreshTokenSessionId(Guid.Parse(original.GetProperty("sessionId").GetString()!));
        UserRefreshTokenSession before = await ReadSessionAsync(id);

        using HttpResponseMessage promoted = await browser.PostAsync("/api/v1/auth/session?rememberMe=true", content: null);
        promoted.EnsureSuccessStatusCode();
        JsonElement payload = await promoted.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(payload.GetProperty("rememberMe").GetBoolean(), "PWA installation must persist the existing browser session.");
        string cookie = Assert.Single(promoted.Headers.GetValues("Set-Cookie"), value => value.StartsWith("fooddiary.browser=", StringComparison.Ordinal));
        Assert.Contains("expires=", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        UserRefreshTokenSession after = await ReadSessionAsync(id);
        Assert.Multiple(() => {
            Assert.True(after.RememberMe);
            Assert.Equal(before.RefreshTokenHash, after.RefreshTokenHash);
            Assert.Equal(before.ExpiresAtUtc, after.ExpiresAtUtc);
        });

        using HttpClient restarted = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        restarted.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
        restarted.DefaultRequestHeaders.Add("X-FoodDiary-Session", id.Value.ToString("D"));
        using HttpResponseMessage restored = await restarted.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        Assert.True((await restored.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("rememberMe").GetBoolean());
        using HttpResponseMessage logout = await restarted.PostAsync("/api/v1/auth/logout", content: null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        using HttpResponseMessage revoked = await restarted.PostAsync("/api/v1/auth/session?rememberMe=true", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);
    }

    private async Task<UserRefreshTokenSession> ReadSessionAsync(RefreshTokenSessionId id) {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        return Assert.IsType<UserRefreshTokenSession>(await scope.ServiceProvider.GetRequiredService<IRefreshTokenSessionWriteRepository>().GetByIdAsync(id));
    }
}
